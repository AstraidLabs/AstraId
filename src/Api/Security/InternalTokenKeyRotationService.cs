using Microsoft.Extensions.Options;
using Api.Options;

namespace Api.Security;

/// <summary>
/// Provides internal token key rotation service functionality.
/// </summary>
public sealed class InternalTokenKeyRotationService : BackgroundService
{
    private readonly InternalTokenKeyRingService _keyRingService;
    private readonly IOptionsMonitor<InternalTokenOptions> _options;

    public InternalTokenKeyRotationService(InternalTokenKeyRingService keyRingService, IOptionsMonitor<InternalTokenOptions> options)
    {
        _keyRingService = keyRingService;
        _options = options;
    }

    /// <summary>
    /// Runs the background loop that rotates and prunes internal signing keys.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _keyRingService.RotateIfDue();
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
