using System.Security.Claims;
using Company.Auth.Contracts;

namespace AuthServer.Authorization;

/// <summary>
/// Provides admin access evaluator functionality.
/// </summary>
public static class AdminAccessEvaluator
{
    public static bool IsAdminUser(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return user.IsInRole("Admin") ||
               user.HasClaim(AuthConstants.ClaimTypes.Permission, AuthConstants.Permissions.SystemAdmin);
    }
}
