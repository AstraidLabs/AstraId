using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace AuthServer.Services;

/// <summary>
/// Provides auth rate limiter functionality.
/// </summary>
public sealed class AuthRateLimiter
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<AuthRateLimiter> _logger;
    private static readonly object CacheLock = new();

    public AuthRateLimiter(IMemoryCache memoryCache, IDistributedCache distributedCache, ILogger<AuthRateLimiter> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
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

        if (TryUseDistributedLimiter(context, key, maxAttempts, window, now, out retryAfterSeconds))
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

    private bool TryUseDistributedLimiter(HttpContext context, string key, int maxAttempts, TimeSpan window, DateTimeOffset now, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;

        if (context.RequestServices.GetService<IConnectionMultiplexer>() is null)
        {
            return false;
        }

        try
        {
            var existing = _distributedCache.GetString(key);
            var state = string.IsNullOrWhiteSpace(existing)
                ? null
                : JsonSerializer.Deserialize<RateLimitState>(existing);

            if (state is null || now - state.WindowStart >= window)
            {
                var reset = JsonSerializer.Serialize(new RateLimitState(1, now));
                _distributedCache.SetString(key, reset, new DistributedCacheEntryOptions { SlidingExpiration = window });
                return false;
            }

            var nextCount = state.Count + 1;
            if (nextCount > maxAttempts)
            {
                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((state.WindowStart + window - now).TotalSeconds));
                return true;
            }

            var updated = JsonSerializer.Serialize(state with { Count = nextCount });
            _distributedCache.SetString(key, updated, new DistributedCacheEntryOptions { SlidingExpiration = window });
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

    /// <summary>
    /// Provides rate limit state functionality.
    /// </summary>
    private sealed record RateLimitState(int Count, DateTimeOffset WindowStart);
}
