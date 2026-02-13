using AuthServer.Models;

namespace AuthServer.Services.Admin.Models;

public sealed record AdminClientDetail(
    string Id,
    string ClientId,
    string? DisplayName,
    string ClientType,
    bool Enabled,
    IReadOnlyList<string> GrantTypes,
    bool PkceRequired,
    string ClientApplicationType,
    bool AllowIntrospection,
    bool AllowUserCredentials,
    IReadOnlyList<string> AllowedScopesForPasswordGrant,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> PostLogoutRedirectUris,
    string? Profile,
    string? PresetId,
    int? PresetVersion,
    bool SystemManaged,
    ClientBranding? Branding);
