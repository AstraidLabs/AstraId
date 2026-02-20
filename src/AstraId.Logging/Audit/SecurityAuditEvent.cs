namespace AstraId.Logging.Audit;

/// <summary>
/// Provides security audit event functionality.
/// </summary>
public sealed class SecurityAuditEvent
{
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Service { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string ActorType { get; set; } = "system";
    public string? ActorId { get; set; }
    public string? Target { get; set; }
    public string? Action { get; set; }
    public string Result { get; set; } = "success";
    public string? ReasonCode { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public string? Ip { get; set; }
    public string? UserAgentHash { get; set; }
}
