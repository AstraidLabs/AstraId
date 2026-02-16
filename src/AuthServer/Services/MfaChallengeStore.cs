using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace AuthServer.Services;

public sealed class MfaChallengeStore
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MfaChallengeStore> _logger;

    public MfaChallengeStore(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        IServiceProvider serviceProvider,
        ILogger<MfaChallengeStore> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public string Create(Guid userId, string? returnUrl)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var state = new MfaChallengeState(userId, returnUrl, DateTimeOffset.UtcNow.Add(DefaultLifetime));
        var cacheKey = BuildKey(token);

        if (!TrySetDistributed(cacheKey, state))
        {
            _memoryCache.Set(token, state, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultLifetime
            });
        }

        return token;
    }

    public bool TryConsume(string token, out MfaChallengeState state)
    {
        state = default!;

        if (TryConsumeDistributed(BuildKey(token), out state))
        {
            return true;
        }

        if (_memoryCache.TryGetValue(token, out state))
        {
            _memoryCache.Remove(token);
            return true;
        }

        return false;
    }

    private bool TrySetDistributed(string key, MfaChallengeState state)
    {
        if (_serviceProvider.GetService<IConnectionMultiplexer>() is null)
        {
            return false;
        }

        try
        {
            _distributedCache.SetString(key, JsonSerializer.Serialize(state), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultLifetime
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store MFA challenge in distributed cache. Falling back to memory cache.");
            return false;
        }
    }

    private bool TryConsumeDistributed(string key, out MfaChallengeState state)
    {
        state = default!;

        if (_serviceProvider.GetService<IConnectionMultiplexer>() is null)
        {
            return false;
        }

        try
        {
            var payload = _distributedCache.GetString(key);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            _distributedCache.Remove(key);
            var parsed = JsonSerializer.Deserialize<MfaChallengeState>(payload);
            if (parsed is null || parsed.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return false;
            }

            state = parsed;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to consume MFA challenge from distributed cache. Falling back to memory cache.");
            return false;
        }
    }

    private static string BuildKey(string token) => $"mfa:{token}";
}

public sealed record MfaChallengeState(Guid UserId, string? ReturnUrl, DateTimeOffset ExpiresAt);
