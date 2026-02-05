namespace AuthServer.Services.Admin.Models;

public sealed record AdminTokenPolicyConfig(
    AdminTokenPolicyValues Policy,
    AdminTokenPolicyGuardrails Guardrails);

public sealed record AdminTokenPolicyValues(
    int AccessTokenMinutes,
    int IdentityTokenMinutes,
    int AuthorizationCodeMinutes,
    int RefreshTokenDays,
    bool RefreshRotationEnabled,
    bool RefreshReuseDetectionEnabled,
    int RefreshReuseLeewaySeconds,
    int ClockSkewSeconds);

public sealed record AdminTokenPolicyGuardrails(
    int MinAccessTokenMinutes,
    int MaxAccessTokenMinutes,
    int MinIdentityTokenMinutes,
    int MaxIdentityTokenMinutes,
    int MinAuthorizationCodeMinutes,
    int MaxAuthorizationCodeMinutes,
    int MinRefreshTokenDays,
    int MaxRefreshTokenDays,
    int MinClockSkewSeconds,
    int MaxClockSkewSeconds);
