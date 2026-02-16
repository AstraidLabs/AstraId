using System.Net;
using Company.Auth.Contracts;
using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace AuthServer.Services.Jobs;

public sealed class SecureDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool _isDevelopment;

    public SecureDashboardAuthorizationFilter(bool isDevelopment)
    {
        _isDevelopment = isDevelopment;
    }

    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        if (user.Identity?.IsAuthenticated == true && user.IsInRole("Admin"))
        {
            var hasPermissionClaims = user.HasClaim(claim => claim.Type == AuthConstants.ClaimTypes.Permission);
            if (!hasPermissionClaims || user.HasClaim(AuthConstants.ClaimTypes.Permission, AuthConstants.Permissions.SystemAdmin))
            {
                return true;
            }
        }

        if (!_isDevelopment)
        {
            return false;
        }

        return httpContext.Connection.RemoteIpAddress is { } ip
               && (ip.Equals(IPAddress.Loopback) || ip.Equals(IPAddress.IPv6Loopback));
    }
}
