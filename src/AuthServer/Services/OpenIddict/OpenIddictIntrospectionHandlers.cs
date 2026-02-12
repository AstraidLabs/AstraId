using AuthServer.Data;
using AuthServer.Services;
using AuthServer.Services.Admin;
using AuthServer.Services.Governance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;

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
            await LogIncidentAsync(httpContext, "oidc_introspection_rejected", context.ClientId, "services_unavailable", context.CancellationToken);
            return;
        }

        if (!await clientStateService.IsClientEnabledAsync(context.ClientId, context.CancellationToken))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "The client is disabled.");
            await LogIncidentAsync(httpContext, "oidc_introspection_rejected", context.ClientId, "client_disabled", context.CancellationToken);
            return;
        }

        var application = await applicationManager.FindByClientIdAsync(context.ClientId, context.CancellationToken);
        if (application is null)
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Unknown client.");
            await LogIncidentAsync(httpContext, "oidc_introspection_rejected", context.ClientId, "unknown_client", context.CancellationToken);
            return;
        }

        var clientType = await applicationManager.GetClientTypeAsync(application, context.CancellationToken);
        if (!string.Equals(clientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.Ordinal))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Only confidential clients can introspect tokens.");
            await LogIncidentAsync(httpContext, "oidc_introspection_rejected", context.ClientId, "public_client", context.CancellationToken);
            return;
        }

        var applicationId = await applicationManager.GetIdAsync(application, context.CancellationToken);
        var state = await dbContext.ClientStates.AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.ApplicationId == applicationId, context.CancellationToken);

        var snapshot = ClientPolicySnapshot.From(state?.OverridesJson);
        var permissions = await applicationManager.GetPermissionsAsync(application, context.CancellationToken);
        var permitted = snapshot.AllowIntrospection
            || permissions.Contains(OpenIddictConstants.Permissions.Endpoints.Introspection, StringComparer.Ordinal);

        if (!permitted)
        {
            context.Reject(
                error: OpenIddictConstants.Errors.UnauthorizedClient,
                description: "The client is not allowed to use the introspection endpoint.");
            await LogIncidentAsync(httpContext, "oidc_introspection_rejected", context.ClientId, "endpoint_not_allowed", context.CancellationToken);
            return;
        }

        await LogIncidentAsync(httpContext, "oidc_introspection_accepted", context.ClientId, null, context.CancellationToken);
    }

    public static async ValueTask ValidateRevocationClientAsync(OpenIddictServerEvents.ValidateRevocationRequestContext context)
    {
        if (string.IsNullOrWhiteSpace(context.ClientId))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Client authentication is required.");
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
            await LogIncidentAsync(httpContext, "oidc_revocation_rejected", context.ClientId, "services_unavailable", context.CancellationToken);
            return;
        }

        if (!await clientStateService.IsClientEnabledAsync(context.ClientId, context.CancellationToken))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "The client is disabled.");
            await LogIncidentAsync(httpContext, "oidc_revocation_rejected", context.ClientId, "client_disabled", context.CancellationToken);
            return;
        }

        var application = await applicationManager.FindByClientIdAsync(context.ClientId, context.CancellationToken);
        if (application is null)
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Unknown client.");
            await LogIncidentAsync(httpContext, "oidc_revocation_rejected", context.ClientId, "unknown_client", context.CancellationToken);
            return;
        }

        var clientType = await applicationManager.GetClientTypeAsync(application, context.CancellationToken);
        if (!string.Equals(clientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.Ordinal))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Only confidential clients can revoke tokens.");
            await LogIncidentAsync(httpContext, "oidc_revocation_rejected", context.ClientId, "public_client", context.CancellationToken);
            return;
        }

        var permissions = await applicationManager.GetPermissionsAsync(application, context.CancellationToken);
        if (!permissions.Contains(OpenIddictConstants.Permissions.Endpoints.Revocation, StringComparer.Ordinal))
        {
            context.Reject(
                error: OpenIddictConstants.Errors.UnauthorizedClient,
                description: "The client is not allowed to use the revocation endpoint.");
            await LogIncidentAsync(httpContext, "oidc_revocation_rejected", context.ClientId, "endpoint_not_allowed", context.CancellationToken);
            return;
        }

        await LogIncidentAsync(httpContext, "oidc_revocation_accepted", context.ClientId, null, context.CancellationToken);
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
