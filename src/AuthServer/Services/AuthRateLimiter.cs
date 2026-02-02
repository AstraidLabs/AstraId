using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace AuthServer.Services;

public sealed class AuthRateLimiter
{
    private readonly IMemoryCache _cache;
    private static readonly object CacheLock = new();

    public AuthRateLimiter(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsLimited(HttpContext context, string action, int maxAttempts, TimeSpan window, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;

        if (maxAttempts <= 0 || window <= TimeSpan.Zero)
        {
            return false;
        }

        var key = $"{action}:{GetClientKey(context)}";
        var now = DateTimeOffset.UtcNow;

        lock (CacheLock)
        {
            if (_cache.TryGetValue<RateLimitState>(key, out var state))
            {
                if (now - state.WindowStart >= window)
                {
                    state = new RateLimitState(1, now);
                    _cache.Set(key, state, window);
                    return false;
                }

                var nextCount = state.Count + 1;
                if (nextCount > maxAttempts)
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((state.WindowStart + window - now).TotalSeconds));
                    return true;
                }

                state = state with { Count = nextCount };
                _cache.Set(key, state, window);
                return false;
            }

            _cache.Set(key, new RateLimitState(1, now), window);
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
