using Api.Services;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

public sealed class EndpointAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public EndpointAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        PolicyMapClient policyMapClient,
        IProblemDetailsService problemDetailsService)
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

        var requiredPermissions = policyMapClient.FindRequiredPermissions(context.Request.Method, path.Value ?? string.Empty);
        if (requiredPermissions is null)
        {
            await WriteProblemDetailsAsync(context, problemDetailsService, StatusCodes.Status403Forbidden, "Forbidden");
            return;
        }

        if (requiredPermissions.Count == 0)
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await WriteProblemDetailsAsync(context, problemDetailsService, StatusCodes.Status401Unauthorized, "Unauthorized");
            return;
        }

        var userPermissions = context.User.FindAll(AuthConstants.ClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!requiredPermissions.All(userPermissions.Contains))
        {
            await WriteProblemDetailsAsync(context, problemDetailsService, StatusCodes.Status403Forbidden, "Forbidden");
            return;
        }

        await _next(context);
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        IProblemDetailsService problemDetailsService,
        int statusCode,
        string title)
    {
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = context.Request.Path
        };

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problemDetails
        });
    }
}
