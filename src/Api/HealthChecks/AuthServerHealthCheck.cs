using Api.Integrations;
using Api.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Api.HealthChecks;

public sealed class AuthServerHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ServiceClientOptions> _optionsMonitor;

    public AuthServerHealthCheck(
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
        var options = _optionsMonitor.Get(ServiceNames.AuthServer);
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return HealthCheckResult.Healthy("AuthServer health check disabled.");
        }

        var path = string.IsNullOrWhiteSpace(options.HealthCheckPath)
            ? ".well-known/openid-configuration"
            : options.HealthCheckPath;

        var client = _httpClientFactory.CreateClient("HealthCheck");
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

        using var response = await client.GetAsync(path.TrimStart('/'), cancellationToken);
        return response.IsSuccessStatusCode
            ? HealthCheckResult.Healthy("AuthServer reachable.")
            : HealthCheckResult.Unhealthy($"AuthServer returned {(int)response.StatusCode}.");
    }
}
