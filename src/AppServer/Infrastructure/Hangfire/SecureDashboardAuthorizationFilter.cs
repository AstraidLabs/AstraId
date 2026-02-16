using System.Net;
using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace AppServer.Infrastructure.Hangfire;

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
        var isLocal = httpContext.Connection.RemoteIpAddress is { } ip
                      && (ip.Equals(IPAddress.Loopback) || ip.Equals(IPAddress.IPv6Loopback));

        if (_isDevelopment)
        {
            return isLocal;
        }

        return isLocal && httpContext.User.Identity?.IsAuthenticated == true;
    }
}
