namespace AuthServer.Data;

public sealed class PrivacyPolicy
{
    public Guid Id { get; set; }
    public int LoginHistoryRetentionDays { get; set; } = 90;
    public int ErrorLogRetentionDays { get; set; } = 30;
    public int TokenRetentionDays { get; set; } = 30;
    public int AuditLogRetentionDays { get; set; } = 365;
    public int DeletionCooldownDays { get; set; } = 14;
    public bool AnonymizeInsteadOfHardDelete { get; set; } = true;
    public bool RequireMfaForDeletionRequest { get; set; } = false;
    public bool RequireRecentReauthForExport { get; set; } = false;
    public DateTime UpdatedUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
