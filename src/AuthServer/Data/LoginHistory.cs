namespace AuthServer.Data;

public sealed class LoginHistory
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? EnteredIdentifier { get; set; }
    public bool Success { get; set; }
    public string? FailureReasonCode { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public string? ClientId { get; set; }
    public string? TraceId { get; set; }

    public ApplicationUser? User { get; set; }
}
