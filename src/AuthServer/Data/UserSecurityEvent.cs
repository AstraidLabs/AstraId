namespace AuthServer.Data;

public sealed class UserSecurityEvent
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? ClientId { get; set; }
    public string? TraceId { get; set; }
}
