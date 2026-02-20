using Api.Options;
using Microsoft.Extensions.Options;

namespace Api.Integrations;

/// <summary>
/// Provides api key handler functionality.
/// </summary>
public sealed class ApiKeyHandler : DelegatingHandler
{
    private readonly string _serviceName;
    private readonly IOptionsMonitor<ServiceClientOptions> _optionsMonitor;

    public ApiKeyHandler(string serviceName, IOptionsMonitor<ServiceClientOptions> optionsMonitor)
    {
        _serviceName = serviceName;
        _optionsMonitor = optionsMonitor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.Get(_serviceName);
        if (!string.IsNullOrWhiteSpace(options.ApiKey) && !request.Headers.Contains("X-Api-Key"))
        {
            request.Headers.Add("X-Api-Key", options.ApiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
