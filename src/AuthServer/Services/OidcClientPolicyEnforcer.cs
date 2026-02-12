using AuthServer.Data;
using AuthServer.Services.Admin;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthServer.Services;

public interface IOidcClientPolicyEnforcer
{
    Task<(bool Allowed, string Error, string Description, string RuleCode)> ValidateAuthorizeAsync(OpenIddictRequest request, CancellationToken cancellationToken);
    Task<(bool Allowed, string Error, string Description, string RuleCode)> ValidateTokenAsync(OpenIddictRequest request, CancellationToken cancellationToken);
}

public sealed class OidcClientPolicyEnforcer : IOidcClientPolicyEnforcer
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ApplicationDbContext _dbContext;

    public OidcClientPolicyEnforcer(IOpenIddictApplicationManager applicationManager, ApplicationDbContext dbContext)
    {
        _applicationManager = applicationManager;
        _dbContext = dbContext;
    }

    public async Task<(bool Allowed, string Error, string Description, string RuleCode)> ValidateAuthorizeAsync(OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var policy = await LoadPolicyAsync(request.ClientId, cancellationToken);
        if (policy is null)
        {
            return (true, string.Empty, string.Empty, string.Empty);
        }

        if (policy.PkceRequired && string.IsNullOrWhiteSpace(request.CodeChallenge))
        {
            return (false, OpenIddictConstants.Errors.InvalidRequest, "PKCE code_challenge is required for this client.", "RULE_SPA_REQUIRE_PKCE");
        }

        if (!string.IsNullOrWhiteSpace(request.RedirectUri)
            && !policy.RedirectUris.Contains(request.RedirectUri, StringComparer.OrdinalIgnoreCase))
        {
            return (false, OpenIddictConstants.Errors.InvalidRequest, "Invalid redirect_uri for this client.", "RULE_REDIRECT_EXACT_MATCH");
        }

        if (request.HasResponseType(OpenIddictConstants.ResponseTypes.Code)
            && !policy.GrantTypes.Contains("authorization_code", StringComparer.OrdinalIgnoreCase))
        {
            return (false, OpenIddictConstants.Errors.UnauthorizedClient, "The client is not allowed to use authorization_code.", "RULE_GRANT_NOT_ALLOWED");
        }

        return (true, string.Empty, string.Empty, string.Empty);
    }

    public async Task<(bool Allowed, string Error, string Description, string RuleCode)> ValidateTokenAsync(OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var policy = await LoadPolicyAsync(request.ClientId, cancellationToken);
        if (policy is null || string.IsNullOrWhiteSpace(request.GrantType))
        {
            return (true, string.Empty, string.Empty, string.Empty);
        }

        if (!policy.GrantTypes.Contains(request.GrantType, StringComparer.OrdinalIgnoreCase))
        {
            return (false, OpenIddictConstants.Errors.UnauthorizedClient, "The client is not allowed to use this grant_type.", "RULE_GRANT_NOT_ALLOWED");
        }

        if (string.Equals(request.GrantType, OpenIddictConstants.GrantTypes.Password, StringComparison.Ordinal)
            && (!policy.IsConfidential
                || !policy.AllowUserCredentials
                || !string.Equals(policy.ClientApplicationType, ClientApplicationTypes.Integration, StringComparison.OrdinalIgnoreCase)))
        {
            return (false, OpenIddictConstants.Errors.UnauthorizedClient, "The client is not allowed to use password grant.", "RULE_PASSWORD_RESTRICTED");
        }

        if (string.Equals(request.GrantType, OpenIddictConstants.GrantTypes.Password, StringComparison.Ordinal))
        {
            var requestedScopes = request.GetScopes().ToArray();
            if (requestedScopes.Length > 0
                && requestedScopes.Except(policy.AllowedScopesForPasswordGrant, StringComparer.OrdinalIgnoreCase).Any())
            {
                return (false, OpenIddictConstants.Errors.InvalidScope, "The requested scopes are not allowed for password grant.", "RULE_PASSWORD_SCOPE_RESTRICTED");
            }
        }

        return (true, string.Empty, string.Empty, string.Empty);
    }

    private async Task<RuntimeClientPolicy?> LoadPolicyAsync(string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var id = await _applicationManager.GetIdAsync(application, cancellationToken);
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var permissions = await _applicationManager.GetPermissionsAsync(application, cancellationToken);
        var requirements = await _applicationManager.GetRequirementsAsync(application, cancellationToken);
        var redirectUris = await _applicationManager.GetRedirectUrisAsync(application, cancellationToken);
        var state = await _dbContext.ClientStates.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicationId == id, cancellationToken);

        var grants = permissions
            .Where(permission => permission.StartsWith(OpenIddictConstants.Permissions.Prefixes.GrantType, StringComparison.Ordinal))
            .Select(permission => permission.Substring(OpenIddictConstants.Permissions.Prefixes.GrantType.Length))
            .ToArray();

        var pkceRequired = requirements.Contains(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange, StringComparer.Ordinal);
        var policySnapshot = ClientPolicySnapshot.From(state?.OverridesJson);

        return new RuntimeClientPolicy(
            grants,
            pkceRequired,
            redirectUris.Select(x => x.ToString()).ToArray(),
            string.Equals(await _applicationManager.GetClientTypeAsync(application, cancellationToken), OpenIddictConstants.ClientTypes.Confidential, StringComparison.Ordinal),
            policySnapshot.ClientApplicationType,
            policySnapshot.AllowUserCredentials,
            policySnapshot.AllowedScopesForPasswordGrant);
    }

    private sealed record RuntimeClientPolicy(
        IReadOnlyList<string> GrantTypes,
        bool PkceRequired,
        IReadOnlyList<string> RedirectUris,
        bool IsConfidential,
        string ClientApplicationType,
        bool AllowUserCredentials,
        IReadOnlyList<string> AllowedScopesForPasswordGrant);
}
