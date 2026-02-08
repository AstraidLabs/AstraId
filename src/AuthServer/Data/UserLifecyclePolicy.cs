namespace AuthServer.Data;

public sealed class UserLifecyclePolicy
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; } = true;
    public int DeactivateAfterDays { get; set; } = 90;
    public int DeleteAfterDays { get; set; } = 365;
    public int? HardDeleteAfterDays { get; set; }
    public bool HardDeleteEnabled { get; set; }
    public int WarnBeforeLogoutMinutes { get; set; } = 5;
    public int IdleLogoutMinutes { get; set; } = 30;
    public DateTime UpdatedUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
