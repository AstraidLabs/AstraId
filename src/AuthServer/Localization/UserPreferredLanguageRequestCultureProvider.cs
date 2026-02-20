using System.Security.Claims;
using AuthServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;

namespace AuthServer.Localization;

/// <summary>
/// Provides user preferred language request culture provider functionality.
/// </summary>
public sealed class UserPreferredLanguageRequestCultureProvider : RequestCultureProvider
{
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (!(httpContext.User.Identity?.IsAuthenticated ?? false))
        {
            return null;
        }

        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return null;
        }

        var userManager = httpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        var language = LanguageTagNormalizer.Normalize(user?.PreferredLanguage);

        if (string.IsNullOrWhiteSpace(user?.PreferredLanguage))
        {
            return null;
        }

        return new ProviderCultureResult(language, language);
    }
}
