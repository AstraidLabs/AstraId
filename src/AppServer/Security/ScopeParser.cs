using System.Security.Claims;

namespace AppServer.Security;

/// <summary>
/// Provides scope parser functionality.
/// </summary>
public static class ScopeParser
{
    public static HashSet<string> GetScopes(ClaimsPrincipal principal)
    {
        var scopes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var claim in principal.FindAll("scope").Concat(principal.FindAll("scp")))
        {
            foreach (var scope in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                scopes.Add(scope);
            }
        }

        return scopes;
    }
}
