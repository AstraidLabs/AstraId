using System.Text.Json;
using AuthServer.Services.Admin.Models;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Admin;

public static class ClientProfileIds
{
    public const string SpaPublic = "SpaPublic";
    public const string WebConfidential = "WebConfidential";
    public const string MobileNativePublic = "MobileNativePublic";
    public const string DesktopNativePublic = "DesktopNativePublic";
    public const string ServiceConfidential = "ServiceConfidential";
}

public sealed record ClientProfileRule(
    string Profile,
    string Summary,
    IReadOnlyList<string> AllowedGrantTypes,
    bool RequiresPkceForAuthorizationCode,
    bool RequiresClientSecret,
    bool AllowsRedirectUris,
    bool AllowOfflineAccess,
    string RedirectPolicy,
    IReadOnlyList<string> RuleCodes);

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

public interface IClientProfileRegistry
{
    IReadOnlyList<ClientProfileRule> GetRules();
    ClientProfileRule? GetRule(string profile);
}

public interface IClientPresetRegistry
{
    IReadOnlyList<ClientPresetDefinition> GetPresets();
    ClientPresetDefinition? GetById(string id);
}

public sealed class ClientProfileRegistry : IClientProfileRegistry
{
    private static readonly IReadOnlyList<ClientProfileRule> Rules =
    [
        new(ClientProfileIds.SpaPublic, "Single-page app using Authorization Code + PKCE.",
            ["authorization_code", "refresh_token"], true, false, true, true,
            "absolute_https_or_loopback", ["RULE_PUBLIC_NO_SECRET", "RULE_SPA_REQUIRE_PKCE", "RULE_SPA_NO_CLIENT_CREDENTIALS"]),
        new(ClientProfileIds.WebConfidential, "Server-side web app with client secret.",
            ["authorization_code", "refresh_token"], false, true, true, true,
            "absolute_https", ["RULE_CONFIDENTIAL_REQUIRE_SECRET"]),
        new(ClientProfileIds.MobileNativePublic, "Native mobile app using PKCE and native redirects.",
            ["authorization_code", "refresh_token"], true, false, true, true,
            "native_loopback_or_custom_scheme", ["RULE_NATIVE_REDIRECT"]),
        new(ClientProfileIds.DesktopNativePublic, "Desktop app using PKCE and native redirects.",
            ["authorization_code", "refresh_token"], true, false, true, true,
            "native_loopback_or_custom_scheme", ["RULE_NATIVE_REDIRECT"]),
        new(ClientProfileIds.ServiceConfidential, "Machine to machine client credentials flow.",
            ["client_credentials"], false, true, false, false,
            "none", ["RULE_SERVICE_ONLY_CLIENT_CREDENTIALS", "RULE_SERVICE_NO_REDIRECT"])
    ];

    public IReadOnlyList<ClientProfileRule> GetRules() => Rules;

    public ClientProfileRule? GetRule(string profile) => Rules.FirstOrDefault(r => string.Equals(r.Profile, profile, StringComparison.Ordinal));
}

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

        return effective with
        {
            ClientType = clientType,
            PkceRequired = pkce,
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
