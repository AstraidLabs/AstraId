namespace AuthServer.Services.Admin.Models;

public sealed record AdminFieldMetadata(string HelpText, string Placeholder, string Example);

public sealed record AdminClientPresetDefaults(
    string ClientType,
    bool PkceRequired,
    IReadOnlyList<string> GrantTypes,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> PostLogoutRedirectUris,
    IReadOnlyList<string> Scopes);

public sealed record AdminClientPresetListItem(string Id, string Name, string Profile, string Summary, int Version);

public sealed record AdminClientPresetDetail(
    string Id,
    string Name,
    string Profile,
    string Summary,
    int Version,
    AdminClientPresetDefaults Defaults,
    IReadOnlySet<string> LockedFields,
    IReadOnlySet<string> AllowedOverrides,
    IReadOnlyDictionary<string, AdminFieldMetadata> FieldMetadata);

public sealed record AdminClientProfileRulesResponse(int Version, IReadOnlyList<AdminClientProfileRuleItem> Profiles);

public sealed record AdminClientProfileRuleItem(
    string Profile,
    string Summary,
    IReadOnlyList<string> AllowedGrantTypes,
    bool RequiresPkceForAuthorizationCode,
    bool RequiresClientSecret,
    bool AllowsRedirectUris,
    bool AllowOfflineAccess,
    string RedirectPolicy,
    IReadOnlyList<string> RuleCodes);

public sealed record AdminClientEffectiveConfig(
    string Profile,
    string PresetId,
    int PresetVersion,
    string ClientType,
    bool PkceRequired,
    IReadOnlyList<string> GrantTypes,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> PostLogoutRedirectUris,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> CorsOrigins);
