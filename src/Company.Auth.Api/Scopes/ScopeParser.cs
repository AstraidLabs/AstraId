using System.Security.Claims;

namespace Company.Auth.Api.Scopes;

public static class ScopeParser
{
    public static HashSet<string> GetScopes(ClaimsPrincipal principal)
    {
        var scopes = new HashSet<string>(StringComparer.Ordinal);

        AppendScopes(scopes, principal.FindAll("scope").Select(claim => claim.Value));

        if (scopes.Count == 0)
        {
            AppendScopes(scopes, principal.FindAll("scp").Select(claim => claim.Value));
        }

        return scopes;
    }

    private static void AppendScopes(HashSet<string> scopes, IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var scope in value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                scopes.Add(scope);
            }
        }
    }
}
