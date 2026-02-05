namespace AuthServer.Services.Admin.Models;

public sealed record AdminSigningKeyRingResponse(
    IReadOnlyList<AdminSigningKeyListItem> Keys,
    DateTime? NextRotationDueUtc,
    DateTimeOffset? NextRotationCheckUtc,
    DateTimeOffset? LastRotationUtc,
    int RetentionDays,
    bool RotationEnabled,
    int RotationIntervalDays,
    int CheckPeriodMinutes);
