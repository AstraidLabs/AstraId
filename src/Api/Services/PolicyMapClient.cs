using System.Net.Http.Json;
using Api.Models;
using Microsoft.Extensions.Options;

namespace Api.Services;


/// <summary>
/// Provides policy map diagnostics functionality.
/// </summary>
public sealed record PolicyMapDiagnostics(
    string EndpointUrl,
    DateTimeOffset? LastRefreshUtc,
    DateTimeOffset? LastFailureUtc,
    string? LastFailureReason,
    string RefreshStatus,
    int EntryCount);

/// <summary>
/// Provides policy map client functionality.
/// </summary>
public sealed class PolicyMapClient
{
    public const string HttpClientName = "PolicyMap";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<PolicyMapOptions> _options;
    private readonly ILogger<PolicyMapClient> _logger;
    private IReadOnlyList<PolicyMapEntry> _entries = Array.Empty<PolicyMapEntry>();
    private DateTimeOffset? _lastRefreshUtc;
    private DateTimeOffset? _lastFailureUtc;
    private string? _lastFailureReason;
    private string _refreshStatus = "never_refreshed";
    private int _entryCount;
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

    /// <summary>
    /// Returns the current in-memory authorization policy map entries.
    /// </summary>
    public IReadOnlyList<PolicyMapEntry> GetEntries()
    {
        lock (_lock)
        {
            return _entries;
        }
    }

    /// <summary>
    /// Returns diagnostic metadata about policy map refresh state.
    /// </summary>
    public PolicyMapDiagnostics GetDiagnostics()
    {
        var options = _options.CurrentValue;
        var endpointUrl = BuildEndpointUrl(options);

        lock (_lock)
        {
            return new PolicyMapDiagnostics(endpointUrl, _lastRefreshUtc, _lastFailureUtc, _lastFailureReason, _refreshStatus, _entryCount);
        }
    }

    /// <summary>
    /// Refreshes policy map entries from the configured remote policy endpoint.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.ApiName))
        {
            _logger.LogWarning("Policy map refresh skipped: missing configuration.");
            SetFailureState("missing_configuration");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _logger.LogWarning("Policy map refresh skipped: missing API key.");
            SetFailureState("missing_api_key");
            return;
        }

        try
        {
            var endpointUrl = BuildEndpointUrl(options);
            var url = new Uri(endpointUrl);
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", options.ApiKey);

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Policy map refresh failed with status {StatusCode} from {Url}.", response.StatusCode, url);
                SetFailureState($"http_{(int)response.StatusCode}");

                return;
            }

            var entries = await response.Content.ReadFromJsonAsync<List<PolicyMapEntry>>(cancellationToken: cancellationToken)
                ?? new List<PolicyMapEntry>();

            lock (_lock)
            {
                _entries = entries;
                _entryCount = entries.Count;
                _lastRefreshUtc = DateTimeOffset.UtcNow;
                _lastFailureReason = null;
                _refreshStatus = "ok";
            }

            _logger.LogInformation("Policy map refreshed: {Count} entries.", entries.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Policy map refresh cancelled.");
        }
        catch (Exception ex)
        {
            SetFailureState($"{ex.GetType().Name}");

            _logger.LogWarning(ex, "Policy map refresh failed due to unexpected error.");
        }
    }

    /// <summary>
    /// Records refresh failure details for diagnostics and health reporting.
    /// </summary>
    private void SetFailureState(string reason)
    {
        lock (_lock)
        {
            _lastFailureUtc = DateTimeOffset.UtcNow;
            _lastFailureReason = reason;
            _refreshStatus = "error";
        }
    }

    /// <summary>
    /// Finds required permissions for a request method and path when an entry matches.
    /// </summary>
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

    /// <summary>
    /// Builds the remote endpoint URL used to fetch policy map entries.
    /// </summary>
    private static string BuildEndpointUrl(PolicyMapOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.ApiName))
        {
            return string.Empty;
        }

        return new Uri(new Uri(options.BaseUrl.TrimEnd('/') + "/"), $"admin/apis/{options.ApiName}/policy-map").ToString();
    }

    /// <summary>
    /// Determines whether a runtime path matches a policy template path pattern.
    /// </summary>
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

    /// <summary>
    /// Splits a path into normalized route segments.
    /// </summary>
    private static string[] SplitPath(string path)
    {
        return path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
