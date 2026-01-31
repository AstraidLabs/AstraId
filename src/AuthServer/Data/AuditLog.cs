namespace AuthServer.Data;

public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? DataJson { get; set; }
}
