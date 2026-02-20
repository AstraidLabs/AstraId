using Company.Auth.Contracts;
using OpenIddict.Abstractions;

namespace AuthServer.Authorization;

/// <summary>
/// Provides auth server scope registry functionality.
/// </summary>
public static class AuthServerScopeRegistry
{
    public const string ApiScope = "api";
    public const string ContentReadScope = "content.read";
    public const string ContentWriteScope = "content.write";

    public static readonly IReadOnlySet<string> AllowedScopes = new HashSet<string>(StringComparer.Ordinal)
    {
        AuthConstants.Scopes.OpenId,
        AuthConstants.Scopes.Profile,
        AuthConstants.Scopes.Email,
        AuthConstants.Scopes.OfflineAccess,
        ApiScope,
        ContentReadScope,
        ContentWriteScope
    };

    public static readonly IReadOnlyList<string> ApiResources =
    [
        ApiScope,
        ContentReadScope,
        ContentWriteScope
    ];

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ScopeClaimMap =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [AuthConstants.Scopes.Profile] = [OpenIddictConstants.Claims.Name],
            [AuthConstants.Scopes.Email] = [OpenIddictConstants.Claims.Email]
        };
}
