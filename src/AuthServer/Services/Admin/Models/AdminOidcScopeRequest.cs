namespace AuthServer.Services.Admin.Models;

public sealed record AdminOidcScopeRequest(
    string Name,
    string? DisplayName,
    string? Description,
    IReadOnlyList<string> Resources,
    IReadOnlyList<string> Claims);
