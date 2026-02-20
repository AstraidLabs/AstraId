namespace AuthServer.Seeding;

/// <summary>
/// Provides api resource seed definition functionality.
/// </summary>
public sealed record ApiResourceSeedDefinition(
    string Name,
    string DisplayName,
    string? BaseUrl,
    bool IsActive);
