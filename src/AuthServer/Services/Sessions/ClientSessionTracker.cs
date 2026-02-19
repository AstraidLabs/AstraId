using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace AuthServer.Services.Sessions;

public sealed class ClientSessionTracker
{
    private readonly IDistributedCache _cache;

    public ClientSessionTracker(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task TrackAsync(string subject, string clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        var cacheKey = BuildKey(subject);
        var payload = await _cache.GetStringAsync(cacheKey, cancellationToken);
        var clients = string.IsNullOrWhiteSpace(payload)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : JsonSerializer.Deserialize<HashSet<string>>(payload) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        clients.Add(clientId);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(clients),
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) },
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetClientsAsync(string subject, CancellationToken cancellationToken)
    {
        var payload = await _cache.GetStringAsync(BuildKey(subject), cancellationToken);
        return string.IsNullOrWhiteSpace(payload)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<HashSet<string>>(payload) ?? Array.Empty<string>();
    }

    public Task ClearAsync(string subject, CancellationToken cancellationToken)
        => _cache.RemoveAsync(BuildKey(subject), cancellationToken);

    private static string BuildKey(string subject) => $"session:clients:{subject}";
}
