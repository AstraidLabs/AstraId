using AuthServer.Data;
using AuthServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Notifications;

/// <summary>
/// Provides email outbox service functionality.
/// </summary>
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

    public async Task<List<EmailOutboxMessage>> GetPendingDueAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.EmailOutboxMessages
            .Where(x => x.Status == EmailOutboxStatus.Pending && x.NextAttemptUtc <= now)
            .OrderBy(x => x.NextAttemptUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
