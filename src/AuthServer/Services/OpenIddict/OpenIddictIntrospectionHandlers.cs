using AuthServer.Data;
using AuthServer.Services;
using AuthServer.Services.Admin;
using AuthServer.Services.Governance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace AuthServer.Services.OpenIddict;

public static class OpenIddictIntrospectionHandlers
{
    public static async ValueTask ValidateIntrospectionClientAsync(OpenIddictServerEvents.ValidateIntrospectionRequestContext context)
    {
        if (string.IsNullOrWhiteSpace(context.ClientId))
        {
            context.Reject(
                error: OpenIddictConstants.Errors.InvalidClient,
                description: "Client authentication is required.");
            await LogAuditAsync(
                context.Transaction.GetHttpRequest()?.HttpContext,
                "oidc.introspection.rejected",
                null,
                "missing_client_auth");
            await LogIncidentAsync(
                context.Transaction.GetHttpRequest()?.HttpContext,
                "oidc_introspection_rejected",
                null,
                "missing_client_auth",
                context.CancellationToken);
            return;
        }

        var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;
        var dbContext = httpContext?.RequestServices.GetService<ApplicationDbContext>();
        var applicationManager = httpContext?.RequestServices.GetService<IOpenIddictApplicationManager>();
        var clientStateService = httpContext?.RequestServices.GetService<IClientStateService>();
        if (dbContext is null || applicationManager is null || clientStateService is null)
        {
            context.Reject(error: OpenIddictConstants.Errors.ServerError, description: "Client policy services are unavailable.");
            await LogAuditAsync(httpContext, "oidc.introspection.rejected", context.ClientId, "services_unavailable");
            await LogIncidentAsync(httpContext, "oidc_introspection_rejected", context.ClientId, "services_unavailable", context.CancellationToken);
            return;
        }

        if (!await clientStateService.IsClientEnabledAsync(context.ClientId, context.CancellationToken))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "The client is disabled.");
            await LogAuditAsync(httpContext, "oidc.introspection.rejected", context.ClientId, "client_disabled");
            await LogIncidentAsync(httpContext, "oidc_introspection_rejected", context.ClientId, "client_disabled", context.CancellationToken);
            return;
        }

        var application = await applicationManager.FindByClientIdAsync(context.ClientId, context.CancellationToken);
        if (application is null)
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Unknown client.");
            await LogAuditAsync(httpContext, "oidc.introspection.rejected", context.ClientId, "unknown_client");
            await LogIncidentAsync(httpContext, "oidc_introspection_rejected", context.ClientId, "unknown_client", context.CancellationToken);
            return;
        }

        var clientType = await applicationManager.GetClientTypeAsync(application, context.CancellationToken);
        if (!string.Equals(clientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.Ordinal))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Only confidential clients can introspect tokens.");
            await LogAuditAsync(httpContext, "oidc.introspection.rejected", context.ClientId, "public_client");
            await LogIncidentAsync(httpContext, "oidc_introspection_rejected", context.ClientId, "public_client", context.CancellationToken);
            return;
        }

        var applicationId = await applicationManager.GetIdAsync(application, context.CancellationToken);
        var state = await dbContext.ClientStates.AsNoTracking()
            .FirstOrDefaultAsync((ClientState entry) => entry.ApplicationId == applicationId, context.CancellationToken);

        var snapshot = ClientPolicySnapshot.From(state?.OverridesJson);
        var permissions = await applicationManager.GetPermissionsAsync(application, context.CancellationToken);
        var permitted = snapshot.AllowIntrospection
            || permissions.Contains(OpenIddictConstants.Permissions.Endpoints.Introspection, StringComparer.Ordinal);

        if (!permitted)
        {
            context.Reject(
                error: OpenIddictConstants.Errors.UnauthorizedClient,
                description: "The client is not allowed to use the introspection endpoint.");
            await LogAuditAsync(httpContext, "oidc.introspection.rejected", context.ClientId, "endpoint_not_allowed");
            await LogIncidentAsync(httpContext, "oidc_introspection_rejected", context.ClientId, "endpoint_not_allowed", context.CancellationToken);
            return;
        }

        await LogAuditAsync(httpContext, "oidc.introspection.accepted", context.ClientId, null);
        await LogIncidentAsync(httpContext, "oidc_introspection_accepted", context.ClientId, null, context.CancellationToken);
    }

    public static async ValueTask ValidateRevocationClientAsync(OpenIddictServerEvents.ValidateRevocationRequestContext context)
    {
        if (string.IsNullOrWhiteSpace(context.ClientId))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Client authentication is required.");
            await LogAuditAsync(
                context.Transaction.GetHttpRequest()?.HttpContext,
                "oidc.revocation.rejected",
                null,
                "missing_client_auth");
            await LogIncidentAsync(
                context.Transaction.GetHttpRequest()?.HttpContext,
                "oidc_revocation_rejected",
                null,
                "missing_client_auth",
                context.CancellationToken);
            return;
        }

        var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;
        var dbContext = httpContext?.RequestServices.GetService<ApplicationDbContext>();
        var applicationManager = httpContext?.RequestServices.GetService<IOpenIddictApplicationManager>();
        var clientStateService = httpContext?.RequestServices.GetService<IClientStateService>();
        if (dbContext is null || applicationManager is null || clientStateService is null)
        {
            context.Reject(error: OpenIddictConstants.Errors.ServerError, description: "Client policy services are unavailable.");
            await LogAuditAsync(httpContext, "oidc.revocation.rejected", context.ClientId, "services_unavailable");
            await LogIncidentAsync(httpContext, "oidc_revocation_rejected", context.ClientId, "services_unavailable", context.CancellationToken);
            return;
        }

        if (!await clientStateService.IsClientEnabledAsync(context.ClientId, context.CancellationToken))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "The client is disabled.");
            await LogAuditAsync(httpContext, "oidc.revocation.rejected", context.ClientId, "client_disabled");
            await LogIncidentAsync(httpContext, "oidc_revocation_rejected", context.ClientId, "client_disabled", context.CancellationToken);
            return;
        }

        var application = await applicationManager.FindByClientIdAsync(context.ClientId, context.CancellationToken);
        if (application is null)
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Unknown client.");
            await LogAuditAsync(httpContext, "oidc.revocation.rejected", context.ClientId, "unknown_client");
            await LogIncidentAsync(httpContext, "oidc_revocation_rejected", context.ClientId, "unknown_client", context.CancellationToken);
            return;
        }

        var clientType = await applicationManager.GetClientTypeAsync(application, context.CancellationToken);
        if (!string.Equals(clientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.Ordinal))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Only confidential clients can revoke tokens.");
            await LogAuditAsync(httpContext, "oidc.revocation.rejected", context.ClientId, "public_client");
            await LogIncidentAsync(httpContext, "oidc_revocation_rejected", context.ClientId, "public_client", context.CancellationToken);
            return;
        }

        var permissions = await applicationManager.GetPermissionsAsync(application, context.CancellationToken);
        if (!permissions.Contains(OpenIddictConstants.Permissions.Endpoints.Revocation, StringComparer.Ordinal))
        {
            context.Reject(
                error: OpenIddictConstants.Errors.UnauthorizedClient,
                description: "The client is not allowed to use the revocation endpoint.");
            await LogAuditAsync(httpContext, "oidc.revocation.rejected", context.ClientId, "endpoint_not_allowed");
            await LogIncidentAsync(httpContext, "oidc_revocation_rejected", context.ClientId, "endpoint_not_allowed", context.CancellationToken);
            return;
        }

        await LogAuditAsync(httpContext, "oidc.revocation.accepted", context.ClientId, null);
        await LogIncidentAsync(httpContext, "oidc_revocation_accepted", context.ClientId, null, context.CancellationToken);
    }

    private static async Task LogAuditAsync(HttpContext? httpContext, string action, string? clientId, string? reason)
    {
        var dbContext = httpContext?.RequestServices.GetService<ApplicationDbContext>();
        var logger = httpContext?.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("OpenIddictEndpointAudit");
        if (dbContext is null)
        {
            return;
        }

        try
        {
            dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TimestampUtc = DateTime.UtcNow,
                ActorUserId = null,
                Action = action,
                TargetType = "OpenIddictEndpoint",
                TargetId = clientId ?? "anonymous",
                DataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    clientId,
                    reason,
                    path = httpContext?.Request.Path.Value,
                    traceId = httpContext?.TraceIdentifier
                })
            });

            using var saveChangesTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await dbContext.SaveChangesAsync(saveChangesTimeout.Token);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to store OpenIddict endpoint audit event {Action}.", action);
        }
    }

    private static async Task LogIncidentAsync(HttpContext? httpContext, string type, string? clientId, string? reason, CancellationToken cancellationToken)
    {
        var incidentService = httpContext?.RequestServices.GetService<TokenIncidentService>();
        if (incidentService is null)
        {
            return;
        }

        await incidentService.LogIncidentAsync(
            type,
            reason is null ? "low" : "medium",
            userId: null,
            clientId,
            detail: new { reason, path = httpContext?.Request.Path.Value, traceId = httpContext?.TraceIdentifier },
            cancellationToken: cancellationToken);
    }
}
