using Api.Options;
using Microsoft.Extensions.Options;

namespace Api.Integrations;

/// <summary>
/// Provides cms client functionality.
/// </summary>
public sealed class CmsClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ServiceClientOptions> _optionsMonitor;

    public CmsClient(HttpClient httpClient, IOptionsMonitor<ServiceClientOptions> optionsMonitor)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<IntegrationPingResult> PingAsync(CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.Get(ServiceNames.Cms);
        var path = string.IsNullOrWhiteSpace(options.HealthCheckPath)
            ? "health"
            : options.HealthCheckPath;

        using var response = await _httpClient.GetAsync(path.TrimStart('/'), cancellationToken);
        return IntegrationPingResult.FromResponse("cms", response);
    }
}
