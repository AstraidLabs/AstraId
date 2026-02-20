using Microsoft.Extensions.Options;

namespace Api.Services;

/// <summary>
/// Provides policy map refresh service functionality.
/// </summary>
public sealed class PolicyMapRefreshService : BackgroundService
{
    private readonly PolicyMapClient _policyMapClient;
    private readonly IOptionsMonitor<PolicyMapOptions> _options;

    public PolicyMapRefreshService(PolicyMapClient policyMapClient, IOptionsMonitor<PolicyMapOptions> options)
    {
        _policyMapClient = policyMapClient;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Continuously refresh the policy map until service shutdown is requested.
        while (!stoppingToken.IsCancellationRequested)
        {
            await _policyMapClient.RefreshAsync(stoppingToken);

            var delay = TimeSpan.FromMinutes(Math.Max(1, _options.CurrentValue.RefreshMinutes));
            // Delay between refresh attempts using a bounded minimum interval.
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
