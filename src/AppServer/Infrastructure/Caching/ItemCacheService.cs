using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace AppServer.Infrastructure.Caching;

/// <summary>
/// Provides item cache service functionality.
/// </summary>
public sealed class ItemCacheService
{
    private readonly IDistributedCache _cache;

    public ItemCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Returns a cached item when available or materializes and stores it using the factory delegate.
    /// </summary>
    public async Task<object> GetOrCreateAsync(string itemId, Func<Task<object>> factory, CancellationToken cancellationToken)
    {
        var cacheKey = $"items:{itemId}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return JsonSerializer.Deserialize<object>(cached) ?? new { id = itemId, source = "cache" };
        }

        var value = await factory();
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(value),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken);

        return value;
    }
}
