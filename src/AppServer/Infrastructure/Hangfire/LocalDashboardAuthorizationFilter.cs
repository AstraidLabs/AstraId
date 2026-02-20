using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace AppServer.Infrastructure.Hangfire;

/// <summary>
/// Provides local dashboard authorization filter functionality.
/// </summary>
public sealed class LocalDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.Connection.RemoteIpAddress is { } ip &&
               (ip.Equals(System.Net.IPAddress.Loopback) || ip.Equals(System.Net.IPAddress.IPv6Loopback));
    }
}
