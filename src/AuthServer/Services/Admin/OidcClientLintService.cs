using AuthServer.Options;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Admin;

public sealed class OidcClientLintService
{
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly IOptions<AuthServerTokenOptions> _tokenOptions;

    public OidcClientLintService(IWebHostEnvironment hostEnvironment, IOptions<AuthServerTokenOptions> tokenOptions)
    {
        _hostEnvironment = hostEnvironment;
        _tokenOptions = tokenOptions;
    }

    public IReadOnlyList<FindingDto> Analyze(AdminClientEffectiveConfig effective)
    {
        var findings = new List<FindingDto>();
        var isDevelopment = _hostEnvironment.IsDevelopment();

        if (effective.GrantTypes.Contains("password", StringComparer.OrdinalIgnoreCase))
        {
            findings.Add(new FindingDto(
                "deprecated",
                "DEPRECATED_PASSWORD_GRANT",
                "Password grant is deprecated",
                "Resource Owner Password Credentials flow should be avoided for new integrations.",
                "grantTypes",
                ["oauth2", "legacy"],
                recommendedFix: "Use authorization_code with PKCE or client_credentials for service clients."));
        }

        if (effective.RedirectUris.Any(uri => uri.Contains('*')))
        {
            findings.Add(new FindingDto(
                isDevelopment ? "risk" : "error",
                "REDIRECT_WILDCARD",
                "Wildcard redirect URI detected",
                "Wildcard redirect URI values are unsafe and can enable token leakage.",
                "redirectUris",
                ["redirect", "open-redirect"],
                recommendedFix: "Replace wildcard entries with exact redirect URIs."));
        }

        if (effective.RedirectUris.Any(uri => Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(new FindingDto(
                isDevelopment ? "risk" : "error",
                "REDIRECT_HTTP_NON_DEV",
                "HTTP redirect URI detected",
                isDevelopment
                    ? "HTTP redirect URI should only be used for local development and loopback testing."
                    : "HTTP redirect URI is not allowed outside development.",
                "redirectUris",
                ["redirect", "transport"],
                recommendedFix: "Use HTTPS redirect URIs except local loopback during development."));
        }

        if (string.Equals(effective.Profile, ClientProfileIds.SpaPublic, StringComparison.Ordinal) && !effective.PkceRequired)
        {
            findings.Add(new FindingDto(
                "error",
                "SPA_PKCE_REQUIRED",
                "PKCE is required for SPA profile",
                "SPA public clients must require PKCE when using authorization code.",
                "pkceRequired",
                ["pkce", "spa"],
                recommendedFix: "Enable PKCE requirement."));
        }

        if (string.Equals(effective.Profile, ClientProfileIds.SpaPublic, StringComparison.Ordinal)
            && string.Equals(effective.ClientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new FindingDto(
                "error",
                "SPA_WITH_SECRET",
                "SPA public profile with confidential client type",
                "SPA public clients must not rely on client secrets.",
                "clientType",
                ["spa", "secret"],
                recommendedFix: "Switch to public client type for SPA profile."));
        }

        if (string.Equals(effective.Profile, ClientProfileIds.ServiceConfidential, StringComparison.Ordinal) && effective.RedirectUris.Count > 0)
        {
            findings.Add(new FindingDto(
                "error",
                "SERVICE_REDIRECT_FORBIDDEN",
                "Service profile should not define redirect URIs",
                "Machine-to-machine clients do not require browser redirects.",
                "redirectUris",
                ["service"],
                recommendedFix: "Remove redirect URI values for service clients."));
        }

        if (effective.CorsOrigins.Any(origin => origin.Contains('*')))
        {
            findings.Add(new FindingDto(
                isDevelopment ? "risk" : "error",
                "CORS_WILDCARD",
                "Wildcard CORS origin detected",
                "CORS origins must be explicit trusted origins.",
                "redirectUris",
                ["cors"],
                recommendedFix: "Replace wildcard CORS origins with explicit origins."));
        }

        if (effective.CorsOrigins.Any(HasCorsPathOrQueryOrFragment))
        {
            findings.Add(new FindingDto(
                "error",
                "CORS_PATH_INVALID",
                "CORS origin contains path or query",
                "CORS origins must be origin-only values (scheme + host + optional port).",
                "redirectUris",
                ["cors"],
                recommendedFix: "Use origin-only values for CORS entries."));
        }

        if (effective.Scopes.Contains("offline_access", StringComparer.OrdinalIgnoreCase)
            && !effective.GrantTypes.Contains("refresh_token", StringComparer.OrdinalIgnoreCase))
        {
            findings.Add(new FindingDto(
                "error",
                "OFFLINE_ACCESS_WITHOUT_REFRESH",
                "offline_access requires refresh tokens",
                "offline_access scope is ineffective without refresh_token grant type.",
                "scopes",
                ["scopes", "refresh-token"],
                recommendedFix: "Enable refresh_token grant type or remove offline_access."));
        }

        var accessTokenMinutes = string.Equals(effective.ClientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase)
            ? _tokenOptions.Value.Confidential.AccessTokenMinutes
            : _tokenOptions.Value.Public.AccessTokenMinutes;
        if (accessTokenMinutes >= 240)
        {
            findings.Add(new FindingDto(
                accessTokenMinutes >= 720 ? "risk" : "warning",
                "ACCESS_TOKEN_LONG_LIFETIME",
                "Access token lifetime is long",
                $"Configured access token lifetime is {accessTokenMinutes} minutes.",
                tags: ["token"],
                recommendedFix: "Consider reducing access token lifetime for better security posture."));
        }

        return findings;
    }

    private static bool HasCorsPathOrQueryOrFragment(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var parsed))
        {
            return true;
        }

        return parsed.AbsolutePath is not "/" or ""
               || !string.IsNullOrEmpty(parsed.Query)
               || !string.IsNullOrEmpty(parsed.Fragment);
    }
}
