namespace AuthServer.Options;

/// <summary>
/// Token lifetime and refresh-token safety defaults applied during token issuance for public and confidential clients.
/// </summary>
public sealed class AuthServerTokenOptions
{
    public const string SectionName = "AuthServer:Tokens";

    public TokenPresetOptions Public { get; set; } = new();
    public TokenPresetOptions Confidential { get; set; } = new();
    public RefreshTokenPolicyOptions RefreshPolicy { get; set; } = new();

    /// <summary>
    /// Provides configuration options for token preset.
    /// </summary>
    public sealed class TokenPresetOptions
    {
        // Lifetimes are in minutes for short-lived bearer credentials.
        public int AccessTokenMinutes { get; set; } = 30;
        public int IdentityTokenMinutes { get; set; } = 30;
        // Refresh token windows are in days to support long-running sessions.
        public int RefreshTokenAbsoluteDays { get; set; } = 30;
        public int RefreshTokenSlidingDays { get; set; } = 7;
    }

    /// <summary>
    /// Provides configuration options for refresh token policy.
    /// </summary>
    public sealed class RefreshTokenPolicyOptions
    {
        public bool RotationEnabled { get; set; } = true;
        public bool ReuseDetectionEnabled { get; set; } = true;
        // Leeway is in seconds to tolerate near-simultaneous retries from the same legitimate client.
        public int ReuseLeewaySeconds { get; set; } = 30;
    }
}
