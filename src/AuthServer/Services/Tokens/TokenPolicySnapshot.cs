namespace AuthServer.Services.Tokens;

/// <summary>
/// Provides token policy snapshot functionality.
/// </summary>
public sealed record TokenPolicySnapshot(
    int AccessTokenMinutes,
    int IdentityTokenMinutes,
    int AuthorizationCodeMinutes,
    int RefreshTokenDays,
    bool RefreshRotationEnabled,
    bool RefreshReuseDetectionEnabled,
    int RefreshReuseLeewaySeconds,
    int ClockSkewSeconds);
