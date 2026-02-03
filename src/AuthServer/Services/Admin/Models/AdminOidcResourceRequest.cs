namespace AuthServer.Services.Admin.Models;

public sealed record AdminOidcResourceRequest(
    string Name,
    string? DisplayName,
    string? Description,
    bool IsActive);
