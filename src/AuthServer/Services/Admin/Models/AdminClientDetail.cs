namespace AuthServer.Services.Admin.Models;

public sealed record AdminClientDetail(
    string Id,
    string ClientId,
    string? DisplayName,
    string ClientType,
    bool Enabled,
    IReadOnlyList<string> GrantTypes,
    bool PkceRequired,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> PostLogoutRedirectUris);
