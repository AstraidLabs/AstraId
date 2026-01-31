using Api.Services;
using Company.Auth.Contracts;

namespace Api.Middleware;

public sealed class EndpointAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public EndpointAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, PolicyMapClient policyMapClient)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (path.StartsWithSegments("/api/public", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var requiredPermissions = policyMapClient.FindRequiredPermissions(context.Request.Method, path.Value ?? string.Empty);
        if (requiredPermissions is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (requiredPermissions.Count == 0)
        {
            await _next(context);
            return;
        }

        var userPermissions = context.User.FindAll(AuthConstants.ClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!requiredPermissions.All(userPermissions.Contains))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }
}
