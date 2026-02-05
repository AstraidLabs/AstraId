namespace AuthServer.Services.Admin.Models;

public sealed record AdminDataProtectionStatus(
    bool KeysPersisted,
    string? KeyPath,
    bool ReadOnly,
    int KeyCount,
    DateTime? LatestKeyActivationUtc,
    DateTime? LatestKeyExpirationUtc);
