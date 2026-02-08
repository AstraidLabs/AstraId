namespace AuthServer.Services.Notifications;

public static class NotificationType
{
    public const string PasswordChanged = "password_changed";
    public const string EmailChangeRequestedOld = "email_change_requested_old";
    public const string EmailChangeRequestedNew = "email_change_requested_new";
    public const string EmailChangedOld = "email_changed_old";
    public const string EmailChangedNew = "email_changed_new";
    public const string MfaEnabled = "mfa_enabled";
    public const string MfaDisabled = "mfa_disabled";
    public const string RecoveryCodesRegenerated = "recovery_codes_regenerated";
    public const string SessionsRevoked = "sessions_revoked";
    public const string InactivityWarning = "inactivity_warning";
    public const string InactivityDeactivated = "inactivity_deactivated";
    public const string InactivityDeletionScheduled = "inactivity_deletion_scheduled";
}
