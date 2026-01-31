using Microsoft.Extensions.Options;

namespace Api.Services;

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
        while (!stoppingToken.IsCancellationRequested)
        {
            await _policyMapClient.RefreshAsync(stoppingToken);

            var delay = TimeSpan.FromMinutes(Math.Max(1, _options.CurrentValue.RefreshMinutes));
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
