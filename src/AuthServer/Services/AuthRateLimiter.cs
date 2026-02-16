using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace AuthServer.Services;

public sealed class AuthRateLimiter
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AuthRateLimiter> _logger;
    private static readonly object CacheLock = new();

    private const string IncrementScript = """
        local current = redis.call('GET', KEYS[1])
        if not current then
            redis.call('SET', KEYS[1], 1, 'PX', ARGV[1], 'NX')
            return {1, 0}
        end

        if not tonumber(current) then
            redis.call('SET', KEYS[1], 1, 'PX', ARGV[1])
            return {1, 0}
        end

        local nextCount = redis.call('INCR', KEYS[1])
        local ttl = redis.call('PTTL', KEYS[1])

        if ttl < 0 then
            redis.call('PEXPIRE', KEYS[1], ARGV[1])
            ttl = tonumber(ARGV[1])
        end

        return {nextCount, ttl}
        """;

    public AuthRateLimiter(IMemoryCache memoryCache, ILogger<AuthRateLimiter> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public bool IsLimited(HttpContext context, string action, int maxAttempts, TimeSpan window, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;

        if (maxAttempts <= 0 || window <= TimeSpan.Zero)
        {
            return false;
        }

        var key = $"auth-rate:{action}:{GetClientKey(context)}";
        var now = DateTimeOffset.UtcNow;

        if (TryUseDistributedLimiter(context, key, maxAttempts, window, out retryAfterSeconds))
        {
            return true;
        }

        lock (CacheLock)
        {
            if (_memoryCache.TryGetValue<RateLimitState>(key, out var state))
            {
                if (now - state.WindowStart >= window)
                {
                    state = new RateLimitState(1, now);
                    _memoryCache.Set(key, state, window);
                    return false;
                }

                var nextCount = state.Count + 1;
                if (nextCount > maxAttempts)
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((state.WindowStart + window - now).TotalSeconds));
                    return true;
                }

                state = state with { Count = nextCount };
                _memoryCache.Set(key, state, window);
                return false;
            }

            _memoryCache.Set(key, new RateLimitState(1, now), window);
            return false;
        }
    }

    private bool TryUseDistributedLimiter(HttpContext context, string key, int maxAttempts, TimeSpan window, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;

        if (context.RequestServices.GetService<IConnectionMultiplexer>() is not { } connectionMultiplexer)
        {
            return false;
        }

        try
        {
            var database = connectionMultiplexer.GetDatabase();
            var result = (RedisResult[]?)database.ScriptEvaluate(
                IncrementScript,
                new RedisKey[] { key },
                new RedisValue[] { (long)window.TotalMilliseconds });

            if (result is null || result.Length < 2)
            {
                return false;
            }

            var nextCount = (long)result[0];
            var ttlMilliseconds = (long)result[1];

            if (nextCount > maxAttempts)
            {
                var ttl = ttlMilliseconds > 0
                    ? TimeSpan.FromMilliseconds(ttlMilliseconds)
                    : window;

                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(ttl.TotalSeconds));
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Distributed auth rate limiter failed. Falling back to in-memory cache.");
            return false;
        }
    }

    private static string GetClientKey(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ip) ? "unknown" : ip;
    }

    private sealed record RateLimitState(int Count, DateTimeOffset WindowStart);
}
