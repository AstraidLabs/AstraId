namespace AuthServer.Services.Admin.Models;

public sealed record AdminOidcResourceListItem(
    Guid Id,
    string Name,
    string? DisplayName,
    string? Description,
    bool IsActive,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
