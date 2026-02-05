using System.Security.Claims;
using AuthServer.Services.Tokens;
using Company.Auth.Contracts;
using OpenIddict.Abstractions;

namespace AuthServer.Authorization;

public static class ClaimDestinations
{
    public static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        if (claim.Type == OpenIddictConstants.Claims.Subject)
        {
            return [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken];
        }

        if (claim.Type == AuthConstants.ClaimTypes.Permission)
        {
            return [OpenIddictConstants.Destinations.AccessToken];
        }

        if (claim.Type == TokenPolicyApplier.RefreshTokenAbsoluteExpiryClaim)
        {
            return [OpenIddictConstants.Destinations.RefreshToken];
        }

        foreach (var mapping in AuthServerScopeRegistry.ScopeClaimMap)
        {
            if (mapping.Value.Contains(claim.Type, StringComparer.Ordinal) && principal.HasScope(mapping.Key))
            {
                return [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken];
            }
        }

        return [OpenIddictConstants.Destinations.AccessToken];
    }
}
