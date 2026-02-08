using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace AuthServer.Services.Security;

public sealed class UserActivityTrackingMiddleware
{
    private static readonly TimeSpan WriteThrottle = TimeSpan.FromMinutes(10);

    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public UserActivityTrackingMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, UserLifecycleService lifecycleService)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            var cacheKey = $"user-activity-write:{userId}";
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                _cache.Set(cacheKey, true, WriteThrottle);
                await lifecycleService.TrackLastSeenAsync(userId, DateTime.UtcNow, context.RequestAborted);
            }
        }

        await _next(context);
    }
}
