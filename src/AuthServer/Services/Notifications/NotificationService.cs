using AuthServer.Data;

namespace AuthServer.Services.Notifications;

public sealed class NotificationService
{
    private readonly EmailOutboxService _outbox;

    public NotificationService(EmailOutboxService outbox)
    {
        _outbox = outbox;
    }

    public Task QueueAsync(Guid? userId, string type, string toEmail, string? toName, string subject, string htmlBody, string? textBody, string? traceId, string? correlationId, string? idempotencyKey, CancellationToken cancellationToken)
    {
        return _outbox.EnqueueAsync(new EmailOutboxMessage
        {
            UserId = userId,
            Type = type,
            ToEmail = toEmail,
            ToName = toName,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
            TraceId = traceId,
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey
        }, cancellationToken);
    }

    public Task NotifyPasswordChangedAsync(ApplicationUser user, string? traceId, CancellationToken cancellationToken)
        => QueueAsync(user.Id, NotificationType.PasswordChanged, user.Email!, user.UserName,
            "Your password was changed",
            "<p>Your account password was changed successfully. If this was not you, contact support immediately.</p>",
            "Your account password was changed successfully. If this was not you, contact support immediately.", traceId, null, $"pwd:{user.Id}:{DateTime.UtcNow:yyyyMMddHHmm}", cancellationToken);

    public Task NotifySessionsRevokedAsync(ApplicationUser user, string reason, string? traceId, CancellationToken cancellationToken)
        => QueueAsync(user.Id, NotificationType.SessionsRevoked, user.Email!, user.UserName,
            "All sessions were revoked",
            $"<p>All active sessions were revoked ({reason}). If this was not you, change your password immediately.</p>",
            $"All active sessions were revoked ({reason}). If this was not you, change your password immediately.", traceId, null, null, cancellationToken);
}
