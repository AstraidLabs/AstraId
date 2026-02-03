namespace AuthServer.Services.Admin.Models;

public sealed record AdminOidcScopeDetail(
    string Id,
    string Name,
    string? DisplayName,
    string? Description,
    IReadOnlyList<string> Resources,
    IReadOnlyList<string> Claims);
