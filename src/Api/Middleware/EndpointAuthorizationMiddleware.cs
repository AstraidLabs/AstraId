using Api.Services;
using Company.Auth.Api.Scopes;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
        IProblemDetailsService problemDetailsService,
        ILogger<EndpointAuthorizationMiddleware> logger,
        IOptions<EndpointAuthorizationOptions> options)
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

        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            logger.LogInformation("Unauthorized request to protected endpoint. Method: {Method}, Path: {Path}", context.Request.Method, path);
            await WriteProblemDetailsAsync(
                context,
                problemDetailsService,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this endpoint.");
            return;
        }

        var requiredScope = options.Value.RequiredScope;
        var scopes = ScopeParser.GetScopes(context.User);
        if (!scopes.Contains(requiredScope))
        {
            logger.LogWarning(
                "Forbidden request due to missing required scope. Method: {Method}, Path: {Path}, RequiredScope: {RequiredScope}",
                context.Request.Method,
                path,
                requiredScope);
            await WriteProblemDetailsAsync(
                context,
                problemDetailsService,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                $"Missing required scope: {requiredScope}");
            return;
        }

        var requiredPermissions = policyMapClient.FindRequiredPermissions(context.Request.Method, path.Value ?? string.Empty);
        if (requiredPermissions is null)
        {
            logger.LogWarning("Endpoint not found in policy map. Method: {Method}, Path: {Path}", context.Request.Method, path);
            await WriteProblemDetailsAsync(
                context,
                problemDetailsService,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "Endpoint is not authorized by policy map.");
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
            logger.LogInformation("Forbidden request due to missing permissions. Method: {Method}, Path: {Path}", context.Request.Method, path);
            await WriteProblemDetailsAsync(
                context,
                problemDetailsService,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "Missing required permission.");
            return;
        }

        await _next(context);
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        IProblemDetailsService problemDetailsService,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problemDetails
        });
    }
}
