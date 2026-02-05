namespace AuthServer.Services.Admin.Models;

public sealed record AdminSigningKeyListItem(
    string Kid,
    string Status,
    DateTime CreatedUtc,
    DateTime? ActivatedUtc,
    DateTime? RetiredUtc,
    string Algorithm,
    string KeyType,
    DateTime? NotBeforeUtc,
    DateTime? NotAfterUtc);
