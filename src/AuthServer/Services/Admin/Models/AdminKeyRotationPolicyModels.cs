namespace AuthServer.Services.Admin.Models;

public sealed record AdminKeyRotationPolicyResponse(
    AdminKeyRotationPolicyValues Policy,
    AdminKeyRotationPolicyGuardrails Guardrails);

public sealed record AdminKeyRotationPolicyValues(
    bool Enabled,
    int RotationIntervalDays,
    int GracePeriodDays,
    DateTime? NextRotationUtc,
    DateTime? LastRotationUtc);

public sealed record AdminKeyRotationPolicyGuardrails(
    int MinRotationIntervalDays,
    int MaxRotationIntervalDays,
    int MinGracePeriodDays,
    int MaxGracePeriodDays,
    bool PreventDisableRotationInProduction);

public sealed record AdminKeyRotationPolicyRequest(
    bool Enabled,
    int RotationIntervalDays,
    int GracePeriodDays,
    bool BreakGlass,
    string? Reason);
