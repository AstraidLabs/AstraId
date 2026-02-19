using Microsoft.Extensions.Options;
using Api.Options;

namespace Api.Security;

public sealed class InternalTokenKeyRotationService : BackgroundService
{
    private readonly InternalTokenKeyRingService _keyRingService;
    private readonly IOptionsMonitor<InternalTokenOptions> _options;

    public InternalTokenKeyRotationService(InternalTokenKeyRingService keyRingService, IOptionsMonitor<InternalTokenOptions> options)
    {
        _keyRingService = keyRingService;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _keyRingService.RotateIfDue();
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
