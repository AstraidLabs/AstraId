using System.Security.Claims;

namespace AppServer.Security;

/// <summary>
/// Provides scope parser functionality.
/// </summary>
public static class ScopeParser
{
    public static HashSet<string> GetScopes(ClaimsPrincipal principal)
    {
        // Collect unique scope values in a set to support fast scope membership checks.
        var scopes = new HashSet<string>(StringComparer.Ordinal);
        // Iterate all supported scope claim types to normalize provider-specific tokens.
        foreach (var claim in principal.FindAll("scope").Concat(principal.FindAll("scp")))
        {
            // Split space-delimited scope claim values into individual scope names.
            foreach (var scope in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                // Add each scope to the set so duplicates collapse automatically.
                scopes.Add(scope);
            }
        }

        return scopes;
    }
}
