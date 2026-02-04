using AuthServer.Data;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Cors;

public sealed class ClientCorsPolicyProvider : ICorsPolicyProvider
{
    private const string PolicyName = "Web";
    private const string CacheKey = "cors.allowed-origins";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ApplicationDbContext _dbContext;

    public ClientCorsPolicyProvider(
        IMemoryCache cache,
        IConfiguration configuration,
        IOpenIddictApplicationManager applicationManager,
        ApplicationDbContext dbContext)
    {
        _cache = cache;
        _configuration = configuration;
        _applicationManager = applicationManager;
        _dbContext = dbContext;
    }

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        if (!string.Equals(policyName, PolicyName, StringComparison.Ordinal))
        {
            return null;
        }

        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            return null;
        }

        var allowedOrigins = await GetAllowedOriginsAsync(context.RequestAborted);
        if (!allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        return new CorsPolicyBuilder()
            .WithOrigins(origin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();
    }

    private async Task<HashSet<string>> GetAllowedOriginsAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out HashSet<string>? cached) && cached is not null)
        {
            return cached;
        }

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configuredOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        foreach (var origin in configuredOrigins)
        {
            if (!string.IsNullOrWhiteSpace(origin))
            {
                allowed.Add(origin.Trim());
            }
        }

        var clientStates = await _dbContext.ClientStates
            .AsNoTracking()
            .ToDictionaryAsync(state => state.ApplicationId, cancellationToken);

        await foreach (var application in _applicationManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var applicationId = await _applicationManager.GetIdAsync(application, cancellationToken);
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                continue;
            }

            if (clientStates.TryGetValue(applicationId, out var state) && !state.Enabled)
            {
                continue;
            }

            var redirectUris = await _applicationManager.GetRedirectUrisAsync(application, cancellationToken);
            foreach (var uri in redirectUris)
            {
                var origin = GetOrigin(uri);
                if (!string.IsNullOrWhiteSpace(origin))
                {
                    allowed.Add(origin);
                }
            }

            var postLogoutUris = await _applicationManager.GetPostLogoutRedirectUrisAsync(application, cancellationToken);
            foreach (var uri in postLogoutUris)
            {
                var origin = GetOrigin(uri);
                if (!string.IsNullOrWhiteSpace(origin))
                {
                    allowed.Add(origin);
                }
            }
        }

        _cache.Set(CacheKey, allowed, CacheDuration);
        return allowed;
    }

    private static string? GetOrigin(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }
}
