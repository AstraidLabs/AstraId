using AuthServer.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Security;

/// <summary>
/// Provides deletion executor service functionality.
/// </summary>
public sealed class DeletionExecutorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeletionExecutorService> _logger;

    public DeletionExecutorService(IServiceScopeFactory scopeFactory, ILogger<DeletionExecutorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecutePendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deletion executor run failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task ExecutePendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<PrivacyGovernanceService>();

        var ready = await db.DeletionRequests
            .Where(r => (r.Status == DeletionRequestStatus.Pending || r.Status == DeletionRequestStatus.Approved)
                        && r.CooldownUntilUtc <= DateTime.UtcNow)
            .OrderBy(r => r.CooldownUntilUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var request in ready)
        {
            await service.ExecuteErasureAsync(request, actorUserId: null, cancellationToken);
        }
    }
}
