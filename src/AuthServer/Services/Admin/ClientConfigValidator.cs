using AuthServer.Services.Admin.Models;
using AuthServer.Services.Admin.Validation;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Admin;

public sealed class ClientConfigValidator
{
    public void Validate(AdminClientEffectiveConfig effective, AdminValidationErrors errors)
    {
        var grants = OidcValidationSpec.NormalizeGrantTypes(effective.GrantTypes, errors, "grantTypes");
        var rule = grants;

        if (string.Equals(effective.Profile, ClientProfileIds.ServiceConfidential, StringComparison.Ordinal))
        {
            if (rule.Except([OpenIddictConstants.GrantTypes.ClientCredentials, OpenIddictConstants.GrantTypes.Password], StringComparer.Ordinal).Any())
            {
                errors.Add("grantTypes", "ServiceConfidential supports only client_credentials and password.");
            }

            if (effective.RedirectUris.Count > 0)
            {
                errors.Add("redirectUris", "ServiceConfidential must not define redirect URIs.");
            }
        }

        if (string.Equals(effective.ClientType, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase)
            && rule.Contains(OpenIddictConstants.GrantTypes.ClientCredentials))
        {
            errors.Add("grantTypes", "Public clients cannot use client_credentials.");
        }

        if (string.Equals(effective.ClientType, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase)
            && rule.Contains(OpenIddictConstants.GrantTypes.Password))
        {
            errors.Add("grantTypes", "Public clients cannot use password grant.");
        }

        if (rule.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode) && effective.RedirectUris.Count == 0)
        {
            errors.Add("redirectUris", "Redirect URIs are required for authorization_code.");
        }

        if (effective.RedirectUris.Count > 10)
        {
            errors.Add("redirectUris", "No more than 10 redirect URIs are allowed.");
        }

        if (effective.PostLogoutRedirectUris.Count > 10)
        {
            errors.Add("postLogoutRedirectUris", "No more than 10 post logout redirect URIs are allowed.");
        }

        if (effective.Scopes.Contains("offline_access", StringComparer.OrdinalIgnoreCase)
            && !rule.Contains(OpenIddictConstants.GrantTypes.RefreshToken))
        {
            errors.Add("scopes", "offline_access requires refresh_token grant type.");
        }

        OidcValidationSpec.ValidatePkceRules(
            string.Equals(effective.ClientType, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase),
            rule,
            effective.PkceRequired,
            errors);

        OidcValidationSpec.NormalizeRedirectUris(effective.RedirectUris, errors, "redirectUris", "Redirect URI");
        OidcValidationSpec.NormalizeRedirectUris(effective.PostLogoutRedirectUris, errors, "postLogoutRedirectUris", "Post logout redirect URI");

        if ((string.Equals(effective.Profile, ClientProfileIds.MobileNativePublic, StringComparison.Ordinal)
                || string.Equals(effective.Profile, ClientProfileIds.DesktopNativePublic, StringComparison.Ordinal))
            && effective.RedirectUris.Any(uri => !IsNativeRedirectAllowed(uri)))
        {
            errors.Add("redirectUris", "Native clients allow loopback http(s) or custom scheme redirects only.");
        }

        if (effective.AllowUserCredentials
            && !string.Equals(effective.ClientApplicationType, ClientApplicationTypes.Integration, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("allowUserCredentials", "Password grant is only allowed for integration clients.");
        }

        if (effective.AllowUserCredentials
            && !rule.Contains(OpenIddictConstants.GrantTypes.Password))
        {
            errors.Add("grantTypes", "allowUserCredentials requires the password grant type.");
        }

        if (!effective.AllowUserCredentials && effective.AllowedScopesForPasswordGrant.Count > 0)
        {
            errors.Add("allowedScopesForPasswordGrant", "Set allowUserCredentials to true before configuring password grant scopes.");
        }

        if (effective.AllowUserCredentials && effective.AllowedScopesForPasswordGrant.Count == 0)
        {
            errors.Add("allowedScopesForPasswordGrant", "At least one allowed scope must be configured for password grant.");
        }

        if (effective.AllowUserCredentials
            && effective.AllowedScopesForPasswordGrant.Any(scope => !effective.Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase)))
        {
            errors.Add("allowedScopesForPasswordGrant", "Password grant scopes must be a subset of client scopes.");
        }
    }

    private static bool IsNativeRedirectAllowed(string raw)
    {
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.IsLoopback && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        return uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps;
    }
}
