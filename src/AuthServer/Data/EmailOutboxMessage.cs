namespace AuthServer.Data;

public enum EmailOutboxStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Cancelled = 3
}

public sealed class EmailOutboxMessage
{
    public Guid Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public EmailOutboxStatus Status { get; set; } = EmailOutboxStatus.Pending;
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public DateTime NextAttemptUtc { get; set; }
    public DateTime? LastAttemptUtc { get; set; }
    public Guid? UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public string? Error { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
}
