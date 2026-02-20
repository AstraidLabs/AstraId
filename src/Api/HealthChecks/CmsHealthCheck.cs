using Api.Integrations;
using Api.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Api.HealthChecks;

/// <summary>
/// Provides cms health check functionality.
/// </summary>
public sealed class CmsHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ServiceClientOptions> _optionsMonitor;

    public CmsHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ServiceClientOptions> optionsMonitor)
    {
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.Get(ServiceNames.Cms);
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return HealthCheckResult.Healthy("CMS health check disabled.");
        }

        var path = string.IsNullOrWhiteSpace(options.HealthCheckPath)
            ? "health"
            : options.HealthCheckPath;

        var client = _httpClientFactory.CreateClient("HealthCheck");
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

        using var response = await client.GetAsync(path.TrimStart('/'), cancellationToken);
        return response.IsSuccessStatusCode
            ? HealthCheckResult.Healthy("CMS reachable.")
            : HealthCheckResult.Unhealthy($"CMS returned {(int)response.StatusCode}.");
    }
}
