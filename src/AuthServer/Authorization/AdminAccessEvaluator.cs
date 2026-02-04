using System.Security.Claims;
using Company.Auth.Contracts;

namespace AuthServer.Authorization;

public static class AdminAccessEvaluator
{
    public static bool IsAdminUser(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return user.IsInRole("Admin") ||
               user.HasClaim(AuthConstants.ClaimTypes.Permission, "system.admin");
    }
}
