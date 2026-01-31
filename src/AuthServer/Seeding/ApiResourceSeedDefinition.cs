namespace AuthServer.Seeding;

public sealed record ApiResourceSeedDefinition(
    string Name,
    string DisplayName,
    string? BaseUrl,
    bool IsActive);
