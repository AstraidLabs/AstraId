namespace AuthServer.Services.Admin.Models;

public sealed record AdminTokenPolicyStatus(
    string? ActiveSigningKid,
    bool RotationEnabled,
    DateTimeOffset? NextRotationCheckUtc,
    AdminTokenPolicyConfig CurrentPolicy);
