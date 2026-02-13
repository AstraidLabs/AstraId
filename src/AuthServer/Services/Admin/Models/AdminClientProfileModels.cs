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

public sealed record AdminClientRuleSectionVisibility(
    bool RedirectUris,
    bool CorsOrigins,
    bool Pkce,
    bool Secrets,
    bool Grants,
    bool Scopes,
    bool TokenOverrides);

public sealed record AdminClientValidationPattern(string Name, string Pattern, string Message);

public sealed record AdminClientProfileRuleItem(
    string Profile,
    string Summary,
    IReadOnlyList<string> AllowedGrantTypes,
    bool RequiresPkceForAuthorizationCode,
    bool RequiresClientSecret,
    bool AllowsRedirectUris,
    bool AllowOfflineAccess,
    string RedirectPolicy,
    IReadOnlyList<string> RuleCodes,
    AdminClientRuleSectionVisibility Sections,
    IReadOnlyList<string> RequiredFields,
    IReadOnlyList<string> ForbiddenFields,
    IReadOnlyList<AdminClientValidationPattern> ValidationPatterns,
    IReadOnlyDictionary<string, string> Explanations);

public sealed record AdminClientEffectiveConfig(
    string Profile,
    string PresetId,
    int PresetVersion,
    string ClientType,
    bool PkceRequired,
    string ClientApplicationType,
    bool AllowIntrospection,
    bool AllowUserCredentials,
    IReadOnlyList<string> AllowedScopesForPasswordGrant,
    IReadOnlyList<string> GrantTypes,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> PostLogoutRedirectUris,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> CorsOrigins);
