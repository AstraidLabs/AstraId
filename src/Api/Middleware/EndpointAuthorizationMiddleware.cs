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
        // Skip non-API routes because this middleware only protects API endpoints.
        if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Allow the explicit public API endpoint to bypass authorization checks.
        if (path.Equals("/api/public", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/public/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Respect endpoint metadata that intentionally allows anonymous requests.
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(context);
            return;
        }

        // Reject unauthenticated callers before evaluating scopes or permissions.
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

        // Resolve all scope values from the user claims for scope enforcement.
        var requiredScope = options.Value.RequiredScope;
        // Build a set of granted scopes for efficient membership checks.
        var scopes = ScopeParser.GetScopes(context.User);
        // Deny access when the required API scope is not granted to the caller.
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
        // Look up required permissions for the current method/path pair.
        var requiredPermissions = policyMapClient.FindRequiredPermissions(context.Request.Method, path.Value ?? string.Empty);
        // Deny requests to unmapped endpoints until policy synchronization adds an entry.
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

        // Skip permission checks when the endpoint intentionally requires no permissions.
        if (requiredPermissions.Count == 0)
        {
            await _next(context);
            return;
        }

        // Materialize caller permissions into a set for case-insensitive matching.
        var userPermissions = context.User.FindAll(AuthConstants.ClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Block access when any endpoint permission is missing from the user token.
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

    /// <summary>
    /// Creates a security audit event payload for endpoint authorization decisions.
    /// </summary>
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

    /// <summary>
    /// Writes a problem-details JSON response for authorization failures.
    /// </summary>
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
