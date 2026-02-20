using System.Text.Json;
using AuthServer.Services.Admin.Models;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Admin;

/// <summary>
/// Provides client profile ids functionality.
/// </summary>
public static class ClientProfileIds
{
    public const string SpaPublic = "SpaPublic";
    public const string WebConfidential = "WebConfidential";
    public const string MobileNativePublic = "MobileNativePublic";
    public const string DesktopNativePublic = "DesktopNativePublic";
    public const string ServiceConfidential = "ServiceConfidential";
}

/// <summary>
/// Provides client profile rule functionality.
/// </summary>
public sealed record ClientProfileRule(
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

/// <summary>
/// Provides client preset definition functionality.
/// </summary>
public sealed record ClientPresetDefinition(
    string Id,
    string Name,
    string Profile,
    string Summary,
    int Version,
    AdminClientPresetDefaults Defaults,
    IReadOnlySet<string> LockedFields,
    IReadOnlySet<string> AllowedOverrides,
    IReadOnlyDictionary<string, AdminFieldMetadata> FieldMetadata);

/// <summary>
/// Defines the contract for client profile registry.
/// </summary>
public interface IClientProfileRegistry
{
    IReadOnlyList<ClientProfileRule> GetRules();
    ClientProfileRule? GetRule(string profile);
}

/// <summary>
/// Defines the contract for client preset registry.
/// </summary>
public interface IClientPresetRegistry
{
    IReadOnlyList<ClientPresetDefinition> GetPresets();
    ClientPresetDefinition? GetById(string id);
}

/// <summary>
/// Provides client profile registry functionality.
/// </summary>
public sealed class ClientProfileRegistry : IClientProfileRegistry
{
    private static readonly IReadOnlyList<ClientProfileRule> Rules =
    [
        BuildRule(ClientProfileIds.SpaPublic, "Single-page app using Authorization Code + PKCE.",
            ["authorization_code", "refresh_token"], true, false, true, true,
            "absolute_https_or_loopback", ["RULE_PUBLIC_NO_SECRET", "RULE_SPA_REQUIRE_PKCE", "RULE_SPA_NO_CLIENT_CREDENTIALS"],
            new(true, true, true, false, true, true, false),
            ["redirectUris", "pkceRequired", "grantTypes"], ["clientSecret"],
            [
                new("corsOrigins", "origin-only", "CORS origins must contain scheme + host (+ optional port) only."),
                new("redirectUris", "exact-match", "Redirect URIs must be exact; wildcards are forbidden.")
            ],
            new Dictionary<string, string> { ["pkce"] = "PKCE is always required for browser public clients.", ["secrets"] = "Public SPA clients must not use a client secret." }),
        BuildRule(ClientProfileIds.WebConfidential, "Server-side web app with client secret.",
            ["authorization_code", "refresh_token"], false, true, true, true,
            "absolute_https", ["RULE_CONFIDENTIAL_REQUIRE_SECRET"],
            new(true, false, false, true, true, true, true),
            ["redirectUris", "grantTypes"], [],
            [new("redirectUris", "https-only-in-prod", "HTTPS redirect URIs are required outside development.")],
            new Dictionary<string, string> { ["secrets"] = "Confidential clients require secret authentication." }),
        BuildRule(ClientProfileIds.MobileNativePublic, "Native mobile app using PKCE and native redirects.",
            ["authorization_code", "refresh_token"], true, false, true, true,
            "native_loopback_or_custom_scheme", ["RULE_NATIVE_REDIRECT"],
            new(true, false, true, false, true, true, false),
            ["redirectUris", "pkceRequired"], ["clientSecret"],
            [new("redirectUris", "loopback-or-custom-scheme", "Use loopback HTTP(S) or a custom URI scheme.")],
            new Dictionary<string, string>()),
        BuildRule(ClientProfileIds.DesktopNativePublic, "Desktop app using PKCE and native redirects.",
            ["authorization_code", "refresh_token"], true, false, true, true,
            "native_loopback_or_custom_scheme", ["RULE_NATIVE_REDIRECT"],
            new(true, false, true, false, true, true, false),
            ["redirectUris", "pkceRequired"], ["clientSecret"],
            [new("redirectUris", "loopback-or-custom-scheme", "Desktop redirects must be loopback HTTP(S) or custom scheme.")],
            new Dictionary<string, string>()),
        BuildRule(ClientProfileIds.ServiceConfidential, "Machine to machine client credentials flow.",
            ["client_credentials"], false, true, false, false,
            "none", ["RULE_SERVICE_ONLY_CLIENT_CREDENTIALS", "RULE_SERVICE_NO_REDIRECT"],
            new(false, false, false, true, true, true, true),
            ["grantTypes"], ["redirectUris", "postLogoutRedirectUris", "pkceRequired"],
            [],
            new Dictionary<string, string> { ["redirectUris"] = "Service clients do not involve browser redirects." })
    ];

    private static ClientProfileRule BuildRule(
        string profile,
        string summary,
        IReadOnlyList<string> grants,
        bool pkce,
        bool requiresSecret,
        bool allowsRedirect,
        bool allowOffline,
        string redirectPolicy,
        IReadOnlyList<string> ruleCodes,
        AdminClientRuleSectionVisibility sections,
        IReadOnlyList<string> required,
        IReadOnlyList<string> forbidden,
        IReadOnlyList<AdminClientValidationPattern> patterns,
        IReadOnlyDictionary<string, string> explanations)
        => new(profile, summary, grants, pkce, requiresSecret, allowsRedirect, allowOffline, redirectPolicy, ruleCodes, sections, required, forbidden, patterns, explanations);

    public IReadOnlyList<ClientProfileRule> GetRules() => Rules;

    public ClientProfileRule? GetRule(string profile) => Rules.FirstOrDefault(r => string.Equals(r.Profile, profile, StringComparison.Ordinal));
}

/// <summary>
/// Provides client preset registry functionality.
/// </summary>
public sealed class ClientPresetRegistry : IClientPresetRegistry
{
    private static readonly IReadOnlyList<ClientPresetDefinition> Presets =
    [
        new("spa-default", "SPA default", ClientProfileIds.SpaPublic, "Public SPA with strict PKCE.", 1,
            new AdminClientPresetDefaults("public", true, ["authorization_code", "refresh_token"], [], [], []),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "clientType", "pkceRequired" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "grantTypes", "redirectUris", "postLogoutRedirectUris", "scopes" },
            FieldMeta()),
        new("web-confidential-default", "Web confidential default", ClientProfileIds.WebConfidential, "Backend web app with secret.", 1,
            new AdminClientPresetDefaults("confidential", false, ["authorization_code", "refresh_token"], [], [], []),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "clientType" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "grantTypes", "redirectUris", "postLogoutRedirectUris", "scopes" },
            FieldMeta()),
        new("mobile-native-default", "Mobile native default", ClientProfileIds.MobileNativePublic, "Mobile PKCE app.", 1,
            new AdminClientPresetDefaults("public", true, ["authorization_code", "refresh_token"], [], [], []),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "clientType", "pkceRequired" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "redirectUris", "postLogoutRedirectUris", "scopes" },
            FieldMeta()),
        new("desktop-native-default", "Desktop native default", ClientProfileIds.DesktopNativePublic, "Desktop PKCE app.", 1,
            new AdminClientPresetDefaults("public", true, ["authorization_code", "refresh_token"], [], [], []),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "clientType", "pkceRequired" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "redirectUris", "postLogoutRedirectUris", "scopes" },
            FieldMeta()),
        new("service-default", "Service default", ClientProfileIds.ServiceConfidential, "Machine client_credentials only.", 1,
            new AdminClientPresetDefaults("confidential", false, ["client_credentials"], [], [], []),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "clientType", "grantTypes", "pkceRequired", "redirectUris", "postLogoutRedirectUris" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "scopes" },
            FieldMeta())
    ];

    public IReadOnlyList<ClientPresetDefinition> GetPresets() => Presets;

    public ClientPresetDefinition? GetById(string id) => Presets.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));

    private static IReadOnlyDictionary<string, AdminFieldMetadata> FieldMeta() => new Dictionary<string, AdminFieldMetadata>(StringComparer.OrdinalIgnoreCase)
    {
        ["clientId"] = new("Client identifier.", "web-spa", "web-spa"),
        ["redirectUris"] = new("One absolute redirect URI per line.", "https://app.example.com/callback", "https://app.example.com/callback"),
        ["scopes"] = new("Allowed scopes for this client.", "openid profile api", "openid")
    };
}

/// <summary>
/// Provides client config composer functionality.
/// </summary>
public sealed class ClientConfigComposer
{
    public AdminClientEffectiveConfig Compose(ClientPresetDefinition preset, JsonElement? overrides)
    {
        var effective = new AdminClientEffectiveConfig(
            preset.Profile,
            preset.Id,
            preset.Version,
            preset.Defaults.ClientType,
            preset.Defaults.PkceRequired,
            ClientApplicationTypes.Web,
            false,
            false,
            [],
            preset.Defaults.GrantTypes.ToArray(),
            preset.Defaults.RedirectUris.ToArray(),
            preset.Defaults.PostLogoutRedirectUris.ToArray(),
            preset.Defaults.Scopes.ToArray(),
            []);

        if (overrides is null || overrides.Value.ValueKind != JsonValueKind.Object)
        {
            return effective;
        }

        var node = overrides.Value;
        var grants = ReadStringArray(node, "grantTypes") ?? effective.GrantTypes;
        var redirects = ReadStringArray(node, "redirectUris") ?? effective.RedirectUris;
        var logoutRedirects = ReadStringArray(node, "postLogoutRedirectUris") ?? effective.PostLogoutRedirectUris;
        var scopes = ReadStringArray(node, "scopes") ?? effective.Scopes;

        var pkce = effective.PkceRequired;
        if (node.TryGetProperty("pkceRequired", out var pkceElement) && pkceElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            pkce = pkceElement.GetBoolean();
        }

        var clientType = effective.ClientType;
        if (node.TryGetProperty("clientType", out var clientTypeElement) && clientTypeElement.ValueKind == JsonValueKind.String)
        {
            clientType = clientTypeElement.GetString() ?? clientType;
        }

        var clientApplicationType = effective.ClientApplicationType;
        if (node.TryGetProperty("clientApplicationType", out var clientApplicationTypeElement) && clientApplicationTypeElement.ValueKind == JsonValueKind.String)
        {
            clientApplicationType = clientApplicationTypeElement.GetString() ?? clientApplicationType;
        }

        var allowIntrospection = effective.AllowIntrospection;
        if (node.TryGetProperty("allowIntrospection", out var allowIntrospectionElement) && allowIntrospectionElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            allowIntrospection = allowIntrospectionElement.GetBoolean();
        }

        var allowUserCredentials = effective.AllowUserCredentials;
        if (node.TryGetProperty("allowUserCredentials", out var allowPasswordElement) && allowPasswordElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            allowUserCredentials = allowPasswordElement.GetBoolean();
        }

        var allowedScopesForPasswordGrant = ReadStringArray(node, "allowedScopesForPasswordGrant") ?? effective.AllowedScopesForPasswordGrant;

        return effective with
        {
            ClientType = clientType,
            PkceRequired = pkce,
            ClientApplicationType = clientApplicationType,
            AllowIntrospection = allowIntrospection,
            AllowUserCredentials = allowUserCredentials,
            AllowedScopesForPasswordGrant = allowedScopesForPasswordGrant,
            GrantTypes = grants,
            RedirectUris = redirects,
            PostLogoutRedirectUris = logoutRedirects,
            Scopes = scopes,
            CorsOrigins = BuildCorsOrigins(preset.Profile, redirects)
        };
    }

    private static string[] BuildCorsOrigins(string profile, IReadOnlyList<string> redirectUris)
    {
        if (!string.Equals(profile, ClientProfileIds.SpaPublic, StringComparison.Ordinal))
        {
            return [];
        }

        return redirectUris
            .Select(uri => Uri.TryCreate(uri, UriKind.Absolute, out var parsed) ? $"{parsed.Scheme}://{parsed.Authority}" : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToArray();
    }

    private static string[]? ReadStringArray(JsonElement source, string property)
    {
        if (!source.TryGetProperty(property, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return element.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
