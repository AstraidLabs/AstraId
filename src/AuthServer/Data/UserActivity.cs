namespace AuthServer.Data;

public sealed class UserActivity
{
    public Guid UserId { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public DateTime? LastPasswordChangeUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
