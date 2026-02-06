namespace AuthServer.Services.Admin.Models;

public sealed record AdminKeyRotationPolicyResponse(
    AdminKeyRotationPolicyValues Policy,
    AdminKeyRotationPolicyGuardrails Guardrails);

public sealed record AdminKeyRotationPolicyValues(
    bool Enabled,
    int RotationIntervalDays,
    int GracePeriodDays,
    int JwksCacheMarginMinutes,
    DateTime? NextRotationUtc,
    DateTime? LastRotationUtc);

public sealed record AdminKeyRotationPolicyGuardrails(
    int MinRotationIntervalDays,
    int MaxRotationIntervalDays,
    int MinGracePeriodDays,
    int MaxGracePeriodDays,
    int MinJwksCacheMarginMinutes,
    int MaxJwksCacheMarginMinutes,
    bool PreventDisableRotationInProduction);

public sealed record AdminKeyRotationPolicyRequest(
    bool Enabled,
    int RotationIntervalDays,
    int GracePeriodDays,
    int JwksCacheMarginMinutes,
    bool BreakGlass,
    string? Reason);
