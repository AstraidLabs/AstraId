using System.Security.Claims;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Tokens;

public sealed class TokenPolicyApplier
{
    public const string RefreshTokenAbsoluteExpiryClaim = "astra:refresh_abs_exp";

    public TokenPolicyApplyResult Apply(
        ClaimsPrincipal principal,
        TokenPolicySnapshot policy,
        DateTimeOffset nowUtc,
        DateTimeOffset? absoluteExpiryOverride = null)
    {
        principal.SetAccessTokenLifetime(TimeSpan.FromMinutes(policy.AccessTokenMinutes));
        principal.SetIdentityTokenLifetime(TimeSpan.FromMinutes(policy.IdentityTokenMinutes));

        var absoluteExpiry = absoluteExpiryOverride
            ?? GetAbsoluteExpiry(principal)
            ?? nowUtc.AddDays(policy.RefreshTokenDays);
        if (absoluteExpiry <= nowUtc)
        {
            return TokenPolicyApplyResult.ExpiredRefreshToken;
        }

        principal.SetClaim(RefreshTokenAbsoluteExpiryClaim, absoluteExpiry.UtcDateTime.ToString("O"));

        var refreshLifetime = absoluteExpiry - nowUtc;
        principal.SetRefreshTokenLifetime(refreshLifetime);
        return TokenPolicyApplyResult.Success;
    }

    public static DateTimeOffset? GetAbsoluteExpiry(ClaimsPrincipal principal)
    {
        var value = principal.GetClaim(RefreshTokenAbsoluteExpiryClaim);
        if (DateTimeOffset.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}

public enum TokenPolicyApplyResult
{
    Success = 0,
    ExpiredRefreshToken = 1
}
