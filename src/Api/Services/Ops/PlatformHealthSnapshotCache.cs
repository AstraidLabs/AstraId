using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using Api.Contracts.Ops;
using Api.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Api.Services.Ops;

/// <summary>
/// Defines the contract for platform health snapshot cache.
/// </summary>
public interface IPlatformHealthSnapshotCache
{
    PlatformHealthDto? GetLatest();
}

/// <summary>
/// Provides platform health snapshot cache functionality.
/// </summary>
public sealed partial class PlatformHealthSnapshotCache : BackgroundService, IPlatformHealthSnapshotCache
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IOptionsMonitor<OpsEndpointsOptions> _optionsMonitor;
    private readonly ILogger<PlatformHealthSnapshotCache> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSuccessUtcByCheck = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();
    private PlatformHealthDto? _latest;

    public PlatformHealthSnapshotCache(
        HealthCheckService healthCheckService,
        IOptionsMonitor<OpsEndpointsOptions> optionsMonitor,
        ILogger<PlatformHealthSnapshotCache> logger,
        IWebHostEnvironment environment)
    {
        _healthCheckService = healthCheckService;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
        _environment = environment;
    }

    public PlatformHealthDto? GetLatest()
    {
        lock (_lock)
        {
            return _latest;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _optionsMonitor.CurrentValue;
            var intervalSeconds = Math.Clamp(options.CheckIntervalSeconds, 10, 300);

            if (options.Enabled)
            {
                await RefreshSnapshotAsync(intervalSeconds, options.CriticalChecks, stoppingToken);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RefreshSnapshotAsync(int intervalSeconds, string[] criticalChecks, CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
        var checkedAt = DateTimeOffset.UtcNow;
        var criticalSet = criticalChecks.Length == 0
            ? null
            : criticalChecks.Where(value => !string.IsNullOrWhiteSpace(value)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var checks = report.Entries.Select(entry =>
        {
            var isCritical = criticalSet is null || criticalSet.Contains(entry.Key);
            if (entry.Value.Status == HealthStatus.Healthy)
            {
                _lastSuccessUtcByCheck[entry.Key] = checkedAt;
            }

            _lastSuccessUtcByCheck.TryGetValue(entry.Key, out var lastSuccessUtc);

            return new PlatformHealthCheckDto
            {
                Key = entry.Key,
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                IsCritical = isCritical,
                DurationMs = Math.Max(0L, (long)entry.Value.Duration.TotalMilliseconds),
                LastSuccessUtc = lastSuccessUtc == default ? null : lastSuccessUtc,
                Message = SanitizeMessage(entry.Value.Description)
            };
        }).ToArray();

        var snapshot = new PlatformHealthDto
        {
            OverallStatus = report.Status.ToString(),
            CheckedAtUtc = checkedAt,
            NextCheckInSeconds = intervalSeconds,
            Environment = _environment.EnvironmentName,
            Service = "Api",
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            Summary = new PlatformHealthSummaryDto
            {
                Healthy = checks.Count(check => string.Equals(check.Status, HealthStatus.Healthy.ToString(), StringComparison.Ordinal)),
                Degraded = checks.Count(check => string.Equals(check.Status, HealthStatus.Degraded.ToString(), StringComparison.Ordinal)),
                Unhealthy = checks.Count(check => string.Equals(check.Status, HealthStatus.Unhealthy.ToString(), StringComparison.Ordinal))
            },
            Checks = checks
                .OrderByDescending(check => GetStatusRank(check.Status))
                .ThenByDescending(check => check.DurationMs)
                .ToArray()
        };

        lock (_lock)
        {
            _latest = snapshot;
        }

        _logger.LogInformation(
            "Platform health check completed. Status={Status}, Healthy={Healthy}, Degraded={Degraded}, Unhealthy={Unhealthy}, DurationMs={DurationMs}",
            snapshot.OverallStatus,
            snapshot.Summary.Healthy,
            snapshot.Summary.Degraded,
            snapshot.Summary.Unhealthy,
            checks.Sum(check => check.DurationMs));
    }

    private static int GetStatusRank(string status) => status switch
    {
        nameof(HealthStatus.Unhealthy) => 3,
        nameof(HealthStatus.Degraded) => 2,
        _ => 1
    };

    private static string? SanitizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var sanitized = message.Trim();
        sanitized = sanitized.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
        sanitized = QueryStringPattern().Replace(sanitized, "$1");

        if (SensitiveTokenPattern().IsMatch(sanitized) || StackTracePattern().IsMatch(sanitized))
        {
            return "Check reported a non-public diagnostic message.";
        }

        if (sanitized.Length > 160)
        {
            sanitized = sanitized[..157] + "...";
        }

        return sanitized;
    }

    [GeneratedRegex("(https?://[^\\s?]+)\\?[^\\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex QueryStringPattern();

    [GeneratedRegex("(token|authorization|cookie|set-cookie|bearer|secret|password|apikey)", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveTokenPattern();

    [GeneratedRegex("\\bat\\s+.+\\sin\\s+.+:line\\s+\\d+", RegexOptions.IgnoreCase)]
    private static partial Regex StackTracePattern();
}
