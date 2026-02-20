using AuthServer.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Security;

/// <summary>
/// Provides retention cleanup service functionality.
/// </summary>
public sealed class RetentionCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetentionCleanupService> _logger;

    public RetentionCleanupService(IServiceScopeFactory scopeFactory, ILogger<RetentionCleanupService> logger)
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
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retention cleanup failed.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var privacy = scope.ServiceProvider.GetRequiredService<PrivacyGovernanceService>();
        var policy = await privacy.GetPolicyAsync(cancellationToken);

        var loginCutoff = DateTime.UtcNow.AddDays(-policy.LoginHistoryRetentionDays);
        var auditCutoff = DateTime.UtcNow.AddDays(-policy.AuditLogRetentionDays);
        var errorCutoff = DateTime.UtcNow.AddDays(-policy.ErrorLogRetentionDays);

        db.LoginHistory.RemoveRange(db.LoginHistory.Where(x => x.TimestampUtc < loginCutoff));
        db.AuditLogs.RemoveRange(db.AuditLogs.Where(x => x.TimestampUtc < auditCutoff));
        db.ErrorLogs.RemoveRange(db.ErrorLogs.Where(x => x.TimestampUtc < errorCutoff));
        await db.SaveChangesAsync(cancellationToken);
    }
}
