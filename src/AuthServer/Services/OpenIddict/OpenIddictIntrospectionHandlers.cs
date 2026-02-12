using AuthServer.Data;
using AuthServer.Services.Admin;
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
            return;
        }

        var dbContext = context.Transaction.GetHttpRequest()?.HttpContext.RequestServices.GetService<ApplicationDbContext>();
        var applicationManager = context.Transaction.GetHttpRequest()?.HttpContext.RequestServices.GetService<IOpenIddictApplicationManager>();
        if (dbContext is null || applicationManager is null)
        {
            context.Reject(error: OpenIddictConstants.Errors.ServerError, description: "Client policy services are unavailable.");
            return;
        }

        var application = await applicationManager.FindByClientIdAsync(context.ClientId, context.CancellationToken);
        if (application is null)
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Unknown client.");
            return;
        }

        var clientType = await applicationManager.GetClientTypeAsync(application, context.CancellationToken);
        if (!string.Equals(clientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.Ordinal))
        {
            context.Reject(error: OpenIddictConstants.Errors.InvalidClient, description: "Only confidential clients can introspect tokens.");
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
        }
    }
}
