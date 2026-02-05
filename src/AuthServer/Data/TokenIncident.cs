namespace AuthServer.Data;

public sealed class TokenIncident
{
    public Guid Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? ClientId { get; set; }
    public string? TraceId { get; set; }
    public string? DetailJson { get; set; }
    public Guid? ActorUserId { get; set; }
}
