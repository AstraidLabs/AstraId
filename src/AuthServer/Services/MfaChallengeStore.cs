using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace AuthServer.Services;

public sealed class MfaChallengeStore
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);
    private readonly IMemoryCache _cache;

    public MfaChallengeStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string Create(Guid userId, string? returnUrl)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var state = new MfaChallengeState(userId, returnUrl, DateTimeOffset.UtcNow.Add(DefaultLifetime));
        _cache.Set(token, state, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultLifetime
        });
        return token;
    }

    public bool TryConsume(string token, out MfaChallengeState state)
    {
        if (_cache.TryGetValue(token, out state))
        {
            _cache.Remove(token);
            return true;
        }

        state = default;
        return false;
    }
}

public sealed record MfaChallengeState(Guid UserId, string? ReturnUrl, DateTimeOffset ExpiresAt);
