using System.Security.Claims;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Tokens;

public sealed class TokenPolicyApplier
{
    public const string RefreshTokenAbsoluteExpiryClaim = "astra:refresh_abs_exp";

    public TokenPolicyApplyResult Apply(
        ClaimsPrincipal principal,
        TokenPreset preset,
        DateTimeOffset nowUtc,
        DateTimeOffset? absoluteExpiryOverride = null)
    {
        principal.SetAccessTokenLifetime(TimeSpan.FromMinutes(preset.AccessTokenMinutes));
        principal.SetIdentityTokenLifetime(TimeSpan.FromMinutes(preset.IdentityTokenMinutes));

        var absoluteExpiry = absoluteExpiryOverride
            ?? GetAbsoluteExpiry(principal)
            ?? nowUtc.AddDays(preset.RefreshTokenAbsoluteDays);
        if (absoluteExpiry <= nowUtc)
        {
            return TokenPolicyApplyResult.ExpiredRefreshToken;
        }

        principal.SetClaim(RefreshTokenAbsoluteExpiryClaim, absoluteExpiry.UtcDateTime.ToString("O"));

        var refreshLifetime = TimeSpan.FromDays(preset.RefreshTokenAbsoluteDays);
        if (preset.RefreshTokenSlidingDays > 0)
        {
            var remaining = absoluteExpiry - nowUtc;
            var sliding = TimeSpan.FromDays(preset.RefreshTokenSlidingDays);
            refreshLifetime = remaining < sliding ? remaining : sliding;
        }

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
