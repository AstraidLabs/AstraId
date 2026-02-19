using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppServer.Security;

public sealed class InternalJwksCache
{
    private readonly ILogger<InternalJwksCache> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<InternalTokenOptions> _options;
    private readonly InternalSigningKeyResolver _resolver;

    public InternalJwksCache(ILogger<InternalJwksCache> logger, IHttpClientFactory httpClientFactory, IOptionsMonitor<InternalTokenOptions> options, InternalSigningKeyResolver resolver)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _resolver = resolver;
    }

    public async Task<bool> RefreshAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        using var client = _httpClientFactory.CreateClient("InternalJwks");
        using var request = new HttpRequestMessage(HttpMethod.Get, options.JwksUrl);
        if (!string.IsNullOrWhiteSpace(options.JwksInternalApiKey) && !string.Equals(options.JwksInternalApiKey, "__REPLACE_ME__", StringComparison.Ordinal))
        {
            request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", options.JwksInternalApiKey);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("JWKS refresh failed with status code {StatusCode}", response.StatusCode);
            return false;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var jwks = new JsonWebKeySet(payload);
        var resolved = jwks.Keys
            .Where(key => !string.IsNullOrWhiteSpace(key.Kid))
            .ToDictionary(key => key.Kid!, key => (SecurityKey)key, StringComparer.Ordinal);

        if (resolved.Count == 0)
        {
            _logger.LogWarning("JWKS refresh returned zero keys.");
            return false;
        }

        _resolver.UpdateKeys(resolved);
        _logger.LogInformation("JWKS refresh successful. Key count: {KeyCount}", resolved.Count);
        return true;
    }
}

public sealed class InternalJwksRefreshService : BackgroundService
{
    private readonly InternalJwksCache _cache;
    private readonly IOptionsMonitor<InternalTokenOptions> _options;

    public InternalJwksRefreshService(InternalJwksCache cache, IOptionsMonitor<InternalTokenOptions> options)
    {
        _cache = cache;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _cache.RefreshAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var minutes = Math.Max(1, _options.CurrentValue.JwksRefreshMinutes);
            await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
            await _cache.RefreshAsync(stoppingToken);
        }
    }
}
