using Api.Options;
using Microsoft.Extensions.Options;

namespace Api.Integrations;

/// <summary>
/// Provides auth server client functionality.
/// </summary>
public sealed class AuthServerClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ServiceClientOptions> _optionsMonitor;

    public AuthServerClient(HttpClient httpClient, IOptionsMonitor<ServiceClientOptions> optionsMonitor)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<IntegrationPingResult> PingAsync(CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.Get(ServiceNames.AuthServer);
        var path = string.IsNullOrWhiteSpace(options.HealthCheckPath)
            ? ".well-known/openid-configuration"
            : options.HealthCheckPath;

        using var response = await _httpClient.GetAsync(path.TrimStart('/'), cancellationToken);
        return IntegrationPingResult.FromResponse("authserver", response);
    }
}
