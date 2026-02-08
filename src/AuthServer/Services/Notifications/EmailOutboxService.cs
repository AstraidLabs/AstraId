using AuthServer.Data;
using AuthServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Notifications;

public sealed class EmailOutboxService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptions<NotificationOptions> _options;

    public EmailOutboxService(ApplicationDbContext dbContext, IOptions<NotificationOptions> options)
    {
        _dbContext = dbContext;
        _options = options;
    }

    public async Task<EmailOutboxMessage> EnqueueAsync(EmailOutboxMessage message, CancellationToken cancellationToken)
    {
        message.Id = message.Id == Guid.Empty ? Guid.NewGuid() : message.Id;
        message.CreatedUtc = DateTime.UtcNow;
        message.NextAttemptUtc = message.NextAttemptUtc == default ? DateTime.UtcNow : message.NextAttemptUtc;
        message.MaxAttempts = message.MaxAttempts <= 0 ? _options.Value.MaxAttempts : message.MaxAttempts;
        message.Status = EmailOutboxStatus.Pending;
        _dbContext.EmailOutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<List<EmailOutboxMessage>> ClaimPendingDueAsync(int batchSize, TimeSpan claimLease, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var claimUntilUtc = now.Add(claimLease <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : claimLease);
        var pendingStatus = (int)EmailOutboxStatus.Pending;

        return await _dbContext.EmailOutboxMessages
            .FromSqlInterpolated($@"
WITH due_messages AS (
    SELECT \"Id\"
    FROM \"EmailOutboxMessages\"
    WHERE \"Status\" = {pendingStatus} AND \"NextAttemptUtc\" <= {now}
    ORDER BY \"NextAttemptUtc\"
    FOR UPDATE SKIP LOCKED
    LIMIT {batchSize}
)
UPDATE \"EmailOutboxMessages\" AS m
SET \"NextAttemptUtc\" = {claimUntilUtc}
FROM due_messages
WHERE m.\"Id\" = due_messages.\"Id\"
RETURNING m.*")
            .ToListAsync(cancellationToken);
    }
}
