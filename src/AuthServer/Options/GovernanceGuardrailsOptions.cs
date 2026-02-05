namespace AuthServer.Options;

public sealed class GovernanceGuardrailsOptions
{
    public const string SectionName = "AuthServer:GovernanceGuardrails";

    public int MinRotationIntervalDays { get; set; } = 7;
    public int MaxRotationIntervalDays { get; set; } = 180;
    public int MinGracePeriodDays { get; set; } = 1;
    public int MaxGracePeriodDays { get; set; } = 60;
    public int MinAccessTokenMinutes { get; set; } = 5;
    public int MaxAccessTokenMinutes { get; set; } = 1440;
    public int MinIdentityTokenMinutes { get; set; } = 5;
    public int MaxIdentityTokenMinutes { get; set; } = 1440;
    public int MinAuthorizationCodeMinutes { get; set; } = 1;
    public int MaxAuthorizationCodeMinutes { get; set; } = 30;
    public int MinRefreshTokenDays { get; set; } = 1;
    public int MaxRefreshTokenDays { get; set; } = 3650;
    public int MinClockSkewSeconds { get; set; } = 0;
    public int MaxClockSkewSeconds { get; set; } = 300;
    public bool PreventDisableRotationInProduction { get; set; } = true;
    public bool RequireReauthForCriticalChanges { get; set; } = false;
}
