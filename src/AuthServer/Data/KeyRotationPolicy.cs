namespace AuthServer.Data;

public sealed class KeyRotationPolicy
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; }
    public int RotationIntervalDays { get; set; }
    public int GracePeriodDays { get; set; }
    public DateTime? NextRotationUtc { get; set; }
    public DateTime? LastRotationUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
