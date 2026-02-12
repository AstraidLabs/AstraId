using System.Net.Http.Json;
using Api.Models;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class PolicyMapClient
{
    public const string HttpClientName = "PolicyMap";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<PolicyMapOptions> _options;
    private readonly ILogger<PolicyMapClient> _logger;
    private IReadOnlyList<PolicyMapEntry> _entries = Array.Empty<PolicyMapEntry>();
    private readonly object _lock = new();

    public PolicyMapClient(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<PolicyMapOptions> options,
        ILogger<PolicyMapClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public IReadOnlyList<PolicyMapEntry> GetEntries()
    {
        lock (_lock)
        {
            return _entries;
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.ApiName))
        {
            _logger.LogWarning("Policy map refresh skipped: missing configuration.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _logger.LogWarning("Policy map refresh skipped: missing API key.");
            return;
        }

        try
        {
            var url = new Uri(new Uri(options.BaseUrl.TrimEnd('/') + "/"), $"admin/apis/{options.ApiName}/policy-map");
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", options.ApiKey);

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Policy map refresh failed with status {StatusCode} from {Url}.", response.StatusCode, url);
                return;
            }

            var entries = await response.Content.ReadFromJsonAsync<List<PolicyMapEntry>>(cancellationToken: cancellationToken)
                ?? new List<PolicyMapEntry>();

            lock (_lock)
            {
                _entries = entries;
            }

            _logger.LogInformation("Policy map refreshed: {Count} entries.", entries.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Policy map refresh cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Policy map refresh failed due to unexpected error.");
        }
    }

    public IReadOnlyCollection<string>? FindRequiredPermissions(string method, string path)
    {
        var entries = GetEntries();
        foreach (var entry in entries)
        {
            if (!string.Equals(entry.Method, method, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsPathMatch(entry.Path, path))
            {
                return entry.RequiredPermissions;
            }
        }

        return null;
    }

    private static bool IsPathMatch(string template, string path)
    {
        var templateSegments = SplitPath(template);
        var pathSegments = SplitPath(path);

        if (templateSegments.Length != pathSegments.Length)
        {
            return false;
        }

        for (var i = 0; i < templateSegments.Length; i++)
        {
            var templateSegment = templateSegments[i];
            if (templateSegment.StartsWith("{") && templateSegment.EndsWith("}"))
            {
                continue;
            }

            if (!string.Equals(templateSegment, pathSegments[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string[] SplitPath(string path)
    {
        return path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
