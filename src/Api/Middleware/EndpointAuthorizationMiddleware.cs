using Api.Services;
using AstraId.Logging.Audit;
using Company.Auth.Api.Scopes;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Middleware;

/// <summary>
/// Enforces endpoint-level authorization using required scope and AuthServer policy-map permissions for /api routes.
/// </summary>
public sealed class EndpointAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public EndpointAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Applies authentication, scope, and permission checks and returns RFC7807 responses for denied API requests.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        PolicyMapClient policyMapClient,
        IProblemDetailsService problemDetailsService,
        ILogger<EndpointAuthorizationMiddleware> logger,
        IOptions<EndpointAuthorizationOptions> options,
        ISecurityAuditLogger securityAuditLogger,
        IWebHostEnvironment environment)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (path.Equals("/api/public", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/public/", StringComparison.OrdinalIgnoreCase))
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
            securityAuditLogger.Log(CreateEvent("api.auth.failure", "failure", "unauthorized", context, environment, path));
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
            securityAuditLogger.Log(CreateEvent("api.scope.denied", "failure", "missing_scope", context, environment, path));
            await WriteProblemDetailsAsync(
                context,
                problemDetailsService,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                $"Missing required scope: {requiredScope}");
            return;
        }

        // Policy map is a deny-by-default contract: unknown endpoints are rejected until explicitly mapped.
        var requiredPermissions = policyMapClient.FindRequiredPermissions(context.Request.Method, path.Value ?? string.Empty);
        if (requiredPermissions is null)
        {
            logger.LogWarning("Endpoint not found in policy map. Method: {Method}, Path: {Path}", context.Request.Method, path);
            securityAuditLogger.Log(CreateEvent("api.policy_map.denied", "failure", "policy_map_missing", context, environment, path));
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
            securityAuditLogger.Log(CreateEvent("api.permission.denied", "failure", "missing_permission", context, environment, path));
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

    private static SecurityAuditEvent CreateEvent(string eventType, string result, string reasonCode, HttpContext context, IWebHostEnvironment environment, PathString path) => new()
    {
        EventType = eventType,
        Service = "Api",
        Environment = environment.EnvironmentName,
        ActorType = context.User.Identity?.IsAuthenticated == true ? "user" : "system",
        ActorId = context.User.FindFirst("sub")?.Value,
        Target = path.Value,
        Action = context.Request.Method,
        Result = result,
        ReasonCode = reasonCode,
        CorrelationId = context.TraceIdentifier,
        TraceId = context.TraceIdentifier,
        Ip = context.Connection.RemoteIpAddress?.ToString()
    };

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
