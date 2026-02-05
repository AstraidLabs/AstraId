namespace AuthServer.Services.Admin.Models;

public sealed record AdminTokenPolicyConfig(
    AdminTokenPreset Public,
    AdminTokenPreset Confidential,
    AdminRefreshTokenPolicy RefreshPolicy);

public sealed record AdminTokenPreset(
    int AccessTokenMinutes,
    int IdentityTokenMinutes,
    int RefreshTokenAbsoluteDays,
    int RefreshTokenSlidingDays);

public sealed record AdminRefreshTokenPolicy(
    bool RotationEnabled,
    bool ReuseDetectionEnabled,
    int ReuseLeewaySeconds);
