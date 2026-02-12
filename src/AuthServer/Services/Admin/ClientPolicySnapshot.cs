using System.Text.Json;

namespace AuthServer.Services.Admin;

public sealed record ClientPolicySnapshot(
    string ClientApplicationType,
    bool AllowIntrospection,
    bool AllowUserCredentials,
    IReadOnlyList<string> AllowedScopesForPasswordGrant)
{
    public static ClientPolicySnapshot From(string? overridesJson)
    {
        var clientApplicationType = ClientApplicationTypes.Web;
        var allowIntrospection = false;
        var allowUserCredentials = false;
        var allowedScopesForPasswordGrant = Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(overridesJson))
        {
            return new ClientPolicySnapshot(clientApplicationType, allowIntrospection, allowUserCredentials, allowedScopesForPasswordGrant);
        }

        using var document = JsonDocument.Parse(overridesJson);
        if (document.RootElement.TryGetProperty("clientApplicationType", out var appTypeElement) && appTypeElement.ValueKind == JsonValueKind.String)
        {
            var parsed = appTypeElement.GetString()?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(parsed) && ClientApplicationTypes.All.Contains(parsed))
            {
                clientApplicationType = parsed;
            }
        }

        if (document.RootElement.TryGetProperty("allowIntrospection", out var introspectionElement)
            && introspectionElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            allowIntrospection = introspectionElement.GetBoolean();
        }

        if (document.RootElement.TryGetProperty("allowUserCredentials", out var passwordElement)
            && passwordElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            allowUserCredentials = passwordElement.GetBoolean();
        }

        if (document.RootElement.TryGetProperty("allowedScopesForPasswordGrant", out var allowedScopesElement)
            && allowedScopesElement.ValueKind == JsonValueKind.Array)
        {
            allowedScopesForPasswordGrant = allowedScopesElement
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return new ClientPolicySnapshot(clientApplicationType, allowIntrospection, allowUserCredentials, allowedScopesForPasswordGrant);
    }
}
