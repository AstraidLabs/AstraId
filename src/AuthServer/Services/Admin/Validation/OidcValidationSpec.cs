using System.Text.RegularExpressions;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Admin.Validation;

public static class OidcValidationSpec
{
    public const int NameMinLength = 3;
    public const int NameMaxLength = 100;
    public const int ClientIdMinLength = 3;
    public const int ClientIdMaxLength = 100;

    public static readonly Regex ScopeNameRegex = new("^[a-z0-9][a-z0-9:_\\.-]*$", RegexOptions.Compiled);
    public static readonly Regex ResourceNameRegex = new("^[a-z0-9][a-z0-9:_\\.-]*$", RegexOptions.Compiled);
    public static readonly Regex ClientIdRegex = new("^[a-zA-Z0-9][a-zA-Z0-9_\\.-]*$", RegexOptions.Compiled);

    public static readonly IReadOnlyDictionary<string, string> GrantTypeMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["authorization_code"] = OpenIddictConstants.GrantTypes.AuthorizationCode,
            ["refresh_token"] = OpenIddictConstants.GrantTypes.RefreshToken,
            ["client_credentials"] = OpenIddictConstants.GrantTypes.ClientCredentials
        };

    public static string NormalizeScopeName(string? name, AdminValidationErrors errors, string field)
    {
        var normalized = name?.Trim().ToLowerInvariant() ?? string.Empty;
        ValidateName(normalized, errors, field, "Scope name", ScopeNameRegex);
        return normalized;
    }

    public static string NormalizeResourceName(string? name, AdminValidationErrors errors, string field)
    {
        var normalized = name?.Trim().ToLowerInvariant() ?? string.Empty;
        ValidateName(normalized, errors, field, "Resource name", ResourceNameRegex);
        return normalized;
    }

    public static string NormalizeClientId(string? clientId, AdminValidationErrors errors, string field)
    {
        var normalized = clientId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            errors.Add(field, "Client ID is required.");
            return normalized;
        }

        if (normalized.Length is < ClientIdMinLength or > ClientIdMaxLength)
        {
            errors.Add(field, $"Client ID must be between {ClientIdMinLength} and {ClientIdMaxLength} characters.");
        }

        if (!ClientIdRegex.IsMatch(normalized))
        {
            errors.Add(field, "Client ID may include letters, numbers, underscores, dots, and dashes.");
        }

        return normalized;
    }

    public static string NormalizeClientType(string? clientType, AdminValidationErrors errors, string field)
    {
        if (string.IsNullOrWhiteSpace(clientType))
        {
            errors.Add(field, "Client type is required.");
            return OpenIddictConstants.ClientTypes.Public;
        }

        if (string.Equals(clientType, "public", StringComparison.OrdinalIgnoreCase)
            || string.Equals(clientType, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
        {
            return OpenIddictConstants.ClientTypes.Public;
        }

        if (string.Equals(clientType, "confidential", StringComparison.OrdinalIgnoreCase)
            || string.Equals(clientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase))
        {
            return OpenIddictConstants.ClientTypes.Confidential;
        }

        errors.Add(field, "Client type must be Public or Confidential.");
        return OpenIddictConstants.ClientTypes.Public;
    }

    public static HashSet<string> NormalizeGrantTypes(
        IReadOnlyList<string>? grantTypes,
        AdminValidationErrors errors,
        string field)
    {
        var normalized = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grantType in grantTypes ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(grantType))
            {
                continue;
            }

            if (!GrantTypeMap.TryGetValue(grantType.Trim(), out var mapped))
            {
                errors.Add(field, $"Unsupported grant type: {grantType}.");
                continue;
            }

            normalized.Add(mapped);
        }

        if (normalized.Count == 0)
        {
            errors.Add(field, "Select at least one grant type.");
        }

        return normalized;
    }

    public static void ValidatePkceRules(
        bool isPublic,
        IReadOnlyCollection<string> grantTypes,
        bool pkceRequired,
        AdminValidationErrors errors)
    {
        if (isPublic && grantTypes.Contains(OpenIddictConstants.GrantTypes.ClientCredentials))
        {
            errors.Add("grantTypes", "Public clients cannot use client_credentials.");
        }

        if (!isPublic && pkceRequired)
        {
            errors.Add("pkceRequired", "PKCE can only be required for public clients.");
        }

        if (pkceRequired && !grantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode))
        {
            errors.Add("pkceRequired", "PKCE requires authorization_code grant type.");
        }
    }

    public static IReadOnlyList<Uri> NormalizeRedirectUris(
        IReadOnlyList<string>? uris,
        AdminValidationErrors errors,
        string field,
        string label)
    {
        var normalized = new List<Uri>();

        foreach (var raw in uris ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (raw.Any(char.IsWhiteSpace))
            {
                errors.Add(field, $"{label} must not contain whitespace: {raw}.");
                continue;
            }

            if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri))
            {
                errors.Add(field, $"{label} is invalid: {raw}.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(uri.Fragment))
            {
                errors.Add(field, $"{label} must not include a fragment: {raw}.");
                continue;
            }

            if (!IsSecureRedirectUri(uri))
            {
                errors.Add(field, $"{label} must use HTTPS unless it is a loopback address: {raw}.");
                continue;
            }

            normalized.Add(uri);
        }

        return normalized
            .DistinctBy(uri => uri.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void ValidateName(
        string name,
        AdminValidationErrors errors,
        string field,
        string label,
        Regex regex)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(field, $"{label} is required.");
            return;
        }

        if (name.Length is < NameMinLength or > NameMaxLength)
        {
            errors.Add(field, $"{label} must be between {NameMinLength} and {NameMaxLength} characters.");
        }

        if (!regex.IsMatch(name))
        {
            errors.Add(field, $"{label} must match {regex}.");
        }
    }

    private static bool IsSecureRedirectUri(Uri uri)
    {
        if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return uri.IsLoopback && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
    }
}
