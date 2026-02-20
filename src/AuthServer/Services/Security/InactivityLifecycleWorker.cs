using AuthServer.Data;
using AuthServer.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Security;

/// <summary>
/// Runs background processing for inactivity lifecycle worker.
/// </summary>
public sealed class InactivityLifecycleWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InactivityLifecycleWorker> _logger;

    public InactivityLifecycleWorker(IServiceScopeFactory scopeFactory, ILogger<InactivityLifecycleWorker> logger)
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
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User inactivity lifecycle worker failed.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lifecycle = scope.ServiceProvider.GetRequiredService<UserLifecycleService>();
        var policyService = scope.ServiceProvider.GetRequiredService<InactivityPolicyService>();
        var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var policy = await policyService.GetAsync(cancellationToken);
        if (!policy.Enabled)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var activities = await db.UserActivities
            .Join(db.Users.Where(u => !u.IsAnonymized), a => a.UserId, u => u.Id, (a, u) => new { Activity = a, User = u })
            .ToListAsync(cancellationToken);

        foreach (var row in activities)
        {
            if (await policyService.IsProtectedAsync(row.User, policy))
            {
                continue;
            }

            var inactiveDays = (now - row.Activity.LastSeenUtc).TotalDays;
            if (inactiveDays >= policy.WarningAfterDays)
            {
                var canSendRepeat = !row.User.LastInactivityWarningSentUtc.HasValue
                    || !policy.WarningRepeatDays.HasValue
                    || row.User.LastInactivityWarningSentUtc.Value <= now.AddDays(-policy.WarningRepeatDays.Value);
                if (canSendRepeat && !string.IsNullOrWhiteSpace(row.User.Email))
                {
                    await notifications.QueueAsync(row.User.Id, NotificationType.InactivityWarning, row.User.Email!, row.User.UserName,
                        "We miss you at AstraId",
                        "<p>You haven't logged in for a while. Please sign in to keep your account active.</p>",
                        "You haven't logged in for a while. Please sign in to keep your account active.",
                        null, null, $"inactive-warning:{row.User.Id}:{now:yyyyMMdd}", cancellationToken);
                    row.User.LastInactivityWarningSentUtc = now;
                }
            }

            if (inactiveDays >= policy.DeactivateAfterDays && row.User.IsActive)
            {
                await lifecycle.DeactivateAsync(row.User, null, cancellationToken);
                if (!string.IsNullOrWhiteSpace(row.User.Email))
                {
                    await notifications.QueueAsync(row.User.Id, NotificationType.InactivityDeactivated, row.User.Email!, row.User.UserName,
                        "Your account was deactivated due to inactivity",
                        "<p>Your account has been deactivated after prolonged inactivity. Contact support to reactivate.</p>",
                        "Your account has been deactivated after prolonged inactivity. Contact support to reactivate.",
                        null, null, $"inactive-deactivated:{row.User.Id}", cancellationToken);
                }
            }

            if (inactiveDays >= policy.DeleteAfterDays)
            {
                row.User.ScheduledDeletionUtc ??= now;
                if (!string.IsNullOrWhiteSpace(row.User.Email))
                {
                    await notifications.QueueAsync(row.User.Id, NotificationType.InactivityDeletionScheduled, row.User.Email!, row.User.UserName,
                        "Account deletion is scheduled",
                        "<p>Your account is scheduled for deletion due to inactivity according to policy.</p>",
                        "Your account is scheduled for deletion due to inactivity according to policy.",
                        null, null, $"inactive-delete-scheduled:{row.User.Id}", cancellationToken);
                }

                if (policy.DeleteMode == InactivityDeleteMode.Anonymize)
                {
                    await lifecycle.AnonymizeAsync(row.User, null, cancellationToken);
                }
                else
                {
                    if (!row.User.IsAnonymized)
                    {
                        await lifecycle.AnonymizeAsync(row.User, null, cancellationToken);
                    }

                    await lifecycle.HardDeleteAsync(row.User, null, cancellationToken);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
