using AuthServer.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Security;

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

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lifecycle = scope.ServiceProvider.GetRequiredService<UserLifecycleService>();

        var policy = await lifecycle.GetPolicyAsync(cancellationToken);
        if (!policy.Enabled)
        {
            return;
        }

        const int batchSize = 100;
        var now = DateTime.UtcNow;

        var deactivateBefore = now.AddDays(-policy.DeactivateAfterDays);
        var deactivateUsers = await db.UserActivities
            .Where(activity => activity.LastSeenUtc <= deactivateBefore)
            .Join(db.Users.Where(user => user.IsActive && !user.IsAnonymized), a => a.UserId, u => u.Id, (_, user) => user)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var user in deactivateUsers)
        {
            await lifecycle.DeactivateAsync(user, null, cancellationToken);
        }

        var anonymizeBefore = now.AddDays(-policy.DeleteAfterDays);
        var anonymizeByInactivity = db.UserActivities
            .Where(activity => activity.LastSeenUtc <= anonymizeBefore)
            .Join(db.Users.Where(user => !user.IsAnonymized), a => a.UserId, u => u.Id, (_, user) => user);

        var requestedDeleteBefore = now.AddDays(-policy.DeleteAfterDays);
        var anonymizeByRequest = db.Users.Where(user => !user.IsAnonymized && user.RequestedDeletionUtc != null && user.RequestedDeletionUtc <= requestedDeleteBefore);

        var anonymizeUsers = await anonymizeByInactivity
            .Union(anonymizeByRequest)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var user in anonymizeUsers)
        {
            await lifecycle.AnonymizeAsync(user, null, cancellationToken);
        }

        if (policy.HardDeleteEnabled && policy.HardDeleteAfterDays is > 0)
        {
            var hardDeleteBefore = now.AddDays(-policy.HardDeleteAfterDays.Value);
            var hardDeleteUsers = await db.UserActivities
                .Where(activity => activity.LastSeenUtc <= hardDeleteBefore)
                .Join(db.Users.Where(user => user.IsAnonymized), a => a.UserId, u => u.Id, (_, user) => user)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            foreach (var user in hardDeleteUsers)
            {
                await lifecycle.HardDeleteAsync(user, null, cancellationToken);
            }
        }
    }
}
