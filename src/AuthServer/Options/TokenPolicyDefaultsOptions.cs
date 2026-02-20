namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for token policy defaults.
/// </summary>
public sealed class TokenPolicyDefaultsOptions
{
    public const string SectionName = "AuthServer:TokenPolicyDefaults";

    public int AccessTokenMinutes { get; set; } = 30;
    public int IdentityTokenMinutes { get; set; } = 30;
    public int AuthorizationCodeMinutes { get; set; } = 5;
    public int RefreshTokenDays { get; set; } = 30;
    public bool RefreshRotationEnabled { get; set; } = true;
    public bool RefreshReuseDetectionEnabled { get; set; } = true;
    public int RefreshReuseLeewaySeconds { get; set; } = 30;
    public int ClockSkewSeconds { get; set; } = 60;
}
