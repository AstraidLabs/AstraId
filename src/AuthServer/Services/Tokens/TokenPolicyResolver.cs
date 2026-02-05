using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Tokens;

public sealed class TokenPolicyResolver
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IMemoryCache _cache;

    public TokenPolicyResolver(IOpenIddictApplicationManager applicationManager, IMemoryCache cache)
    {
        _applicationManager = applicationManager;
        _cache = cache;
    }

    public async Task<string> GetClientTypeAsync(string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return OpenIddictConstants.ClientTypes.Public;
        }

        if (_cache.TryGetValue(clientId, out string? cached) && !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        var type = application is null
            ? OpenIddictConstants.ClientTypes.Public
            : await _applicationManager.GetClientTypeAsync(application, cancellationToken)
                ?? OpenIddictConstants.ClientTypes.Public;

        _cache.Set(clientId, type, TimeSpan.FromMinutes(5));
        return type;
    }
}
