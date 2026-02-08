namespace AuthServer.Data;

public enum InactivityDeleteMode
{
    Anonymize = 0,
    HardDelete = 1
}

public sealed class InactivityPolicy
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; } = true;
    public int WarningAfterDays { get; set; } = 60;
    public int DeactivateAfterDays { get; set; } = 90;
    public int DeleteAfterDays { get; set; } = 365;
    public int? WarningRepeatDays { get; set; }
    public InactivityDeleteMode DeleteMode { get; set; } = InactivityDeleteMode.Anonymize;
    public bool ProtectAdmins { get; set; } = true;
    public string ProtectedRoles { get; set; } = "Admin";
    public DateTime UpdatedUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
