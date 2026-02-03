namespace AuthServer.Services.Admin.Models;

public sealed record AdminApiResourceDetail(
    Guid Id,
    string Name,
    string DisplayName,
    string? BaseUrl,
    bool IsActive,
    string? ApiKey);
