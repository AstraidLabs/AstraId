namespace AuthServer.Services.Tokens;

public sealed record TokenPolicySnapshot(
    TokenPreset Public,
    TokenPreset Confidential,
    RefreshTokenPolicy RefreshPolicy);

public sealed record TokenPreset(
    int AccessTokenMinutes,
    int IdentityTokenMinutes,
    int RefreshTokenAbsoluteDays,
    int RefreshTokenSlidingDays);

public sealed record RefreshTokenPolicy(
    bool RotationEnabled,
    bool ReuseDetectionEnabled,
    int ReuseLeewaySeconds);
