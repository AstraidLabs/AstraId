using AuthServer.Seeding;

namespace AuthServer.Services;

public sealed class ReturnUrlValidator
{
    private static readonly HashSet<string> AllowedRedirectUris = AuthServerDefinitions.Clients
        .SelectMany(client => client.RedirectUris)
        .Select(uri => uri.ToString())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public bool IsValidReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return false;
        }

        if (IsLocalUrl(returnUrl))
        {
            return true;
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute))
        {
            return AllowedRedirectUris.Contains(absolute.ToString());
        }

        return false;
    }

    private static bool IsLocalUrl(string returnUrl)
    {
        if (!returnUrl.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        if (returnUrl.StartsWith("//", StringComparison.Ordinal) || returnUrl.StartsWith("/\\", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }
}
