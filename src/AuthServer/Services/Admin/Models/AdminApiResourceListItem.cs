namespace AuthServer.Services.Admin.Models;

public sealed record AdminApiResourceListItem(
    Guid Id,
    string Name,
    string DisplayName,
    string? BaseUrl,
    bool IsActive);
