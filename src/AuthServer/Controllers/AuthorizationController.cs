using System.Security.Claims;
using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using AuthServer.Authorization;
using AuthServer.Data;
using AuthServer.Services;
using AuthServer.Services.Governance;
using AuthServer.Services.Tokens;
using AuthServer.Services.Security;
using Microsoft.EntityFrameworkCore;
using AuthServer.Services.Admin;
using AuthServer.Options;
using AuthServer.Localization;
using Microsoft.Extensions.Localization;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;

namespace AuthServer.Controllers;

[ApiController]
public class AuthorizationController : ControllerBase
{
    private static readonly IReadOnlySet<string> AllowedScopes = AuthServerScopeRegistry.AllowedScopes;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPermissionService _permissionService;
    private readonly UiUrlBuilder _uiUrlBuilder;
    private readonly IClientStateService _clientStateService;
    private readonly TokenPolicyService _tokenPolicyService;
    private readonly TokenPolicyApplier _tokenPolicyApplier;
    private readonly RefreshTokenReuseDetectionService _refreshTokenReuseDetection;
    private readonly RefreshTokenReuseRemediationService _refreshTokenReuseRemediation;
    private readonly LoginHistoryService _loginHistoryService;
    private readonly TokenIncidentService _incidentService;
    private readonly IOidcClientPolicyEnforcer _policyEnforcer;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AuthorizationController> _logger;
    private readonly IStringLocalizer<AuthMessages> _localizer;
    private readonly AuthServerAuthFeaturesOptions _authFeatures;

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPermissionService permissionService,
        UiUrlBuilder uiUrlBuilder,
        IClientStateService clientStateService,
        TokenPolicyService tokenPolicyService,
        TokenPolicyApplier tokenPolicyApplier,
        RefreshTokenReuseDetectionService refreshTokenReuseDetection,
        RefreshTokenReuseRemediationService refreshTokenReuseRemediation,
        LoginHistoryService loginHistoryService,
        TokenIncidentService incidentService,
        IOidcClientPolicyEnforcer policyEnforcer,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IAntiforgery antiforgery,
        IOptions<AuthServerAuthFeaturesOptions> authFeatures,
        ILogger<AuthorizationController> logger,
        IStringLocalizer<AuthMessages> localizer)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _permissionService = permissionService;
        _uiUrlBuilder = uiUrlBuilder;
        _clientStateService = clientStateService;
        _tokenPolicyService = tokenPolicyService;
        _tokenPolicyApplier = tokenPolicyApplier;
        _refreshTokenReuseDetection = refreshTokenReuseDetection;
        _refreshTokenReuseRemediation = refreshTokenReuseRemediation;
        _loginHistoryService = loginHistoryService;
        _incidentService = incidentService;
        _policyEnforcer = policyEnforcer;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _antiforgery = antiforgery;
        _authFeatures = authFeatures.Value;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            _logger.LogWarning("Authorization request missing OpenIddict request data.");
            return BadRequest(CreateErrorResponse(OpenIddictConstants.Errors.InvalidRequest, L("Oidc.Authorize.InvalidRequest", "The authorization request is invalid.")));
        }

        if (!await _clientStateService.IsClientEnabledAsync(request.ClientId, HttpContext.RequestAborted))
        {
            _logger.LogInformation("Authorization request rejected because client {ClientId} is disabled.", request.ClientId);
            return Forbid(CreateClientDisabledProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var authorizePolicy = await _policyEnforcer.ValidateAuthorizeAsync(request, HttpContext.RequestAborted);
        if (!authorizePolicy.Allowed)
        {
            await _incidentService.LogIncidentAsync(
                "oidc_client_rule_violation",
                "medium",
                null,
                request.ClientId,
                new { request.ClientId, ruleCode = authorizePolicy.RuleCode, path = HttpContext.Request.Path, traceId = HttpContext.TraceIdentifier },
                cancellationToken: HttpContext.RequestAborted);
            return Forbid(new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictConstants.Parameters.Error] = authorizePolicy.Error,
                [OpenIddictConstants.Parameters.ErrorDescription] = authorizePolicy.Description
            }), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            var redirectUri = Request.PathBase + Request.Path + Request.QueryString;
            return Redirect(_uiUrlBuilder.BuildLoginUrl(redirectUri));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null || !user.IsActive || user.IsAnonymized)
        {
            _logger.LogWarning("Authorization request rejected because user is missing or inactive.");
            return Forbid(CreateUserDisabledProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var requestedScopes = request.GetScopes().Intersect(AllowedScopes).ToArray();
        var app = string.IsNullOrWhiteSpace(request.ClientId)
            ? null
            : await _applicationManager.FindByClientIdAsync(request.ClientId, HttpContext.RequestAborted);
        var applicationId = app is null ? null : await _applicationManager.GetIdAsync(app, HttpContext.RequestAborted);
        var existingAuthorization = await FindValidAuthorizationAsync(user.Id, applicationId, requestedScopes, HttpContext.RequestAborted);
        var promptValues = GetPromptValues(request);
        var forceConsent = promptValues.Contains("consent", StringComparer.Ordinal);
        var noPrompt = promptValues.Contains("none", StringComparer.Ordinal);

        if (forceConsent || existingAuthorization is null)
        {
            if (noPrompt)
            {
                return Forbid(new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictConstants.Parameters.Error] = OpenIddictConstants.Errors.InteractionRequired,
                    [OpenIddictConstants.Parameters.ErrorDescription] = L("Oidc.Consent.PromptNone", "User consent is required.")
                }), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var clientName = app is null
                ? request.ClientId ?? "Unknown client"
                : await _applicationManager.GetDisplayNameAsync(app, HttpContext.RequestAborted) ?? request.ClientId ?? "Unknown client";
            return RenderConsentPage(clientName, requestedScopes);
        }

        var policy = await _tokenPolicyService.GetEffectivePolicyAsync(HttpContext.RequestAborted);

        var principal = await CreatePrincipalAsync(
            user,
            requestedScopes,
            policy,
            refreshAbsoluteExpiry: null);
        principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(existingAuthorization, HttpContext.RequestAborted));
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/authorize")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitConsent([FromForm] string decision, [FromForm] bool remember)
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return BadRequest(CreateErrorResponse(OpenIddictConstants.Errors.InvalidRequest, L("Oidc.Authorize.InvalidRequest", "The authorization request is invalid.")));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null || !user.IsActive || user.IsAnonymized)
        {
            return Forbid(CreateUserDisabledProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (string.Equals(decision, "deny", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid(new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictConstants.Parameters.Error] = OpenIddictConstants.Errors.AccessDenied,
                [OpenIddictConstants.Parameters.ErrorDescription] = "The resource owner denied the authorization request."
            }), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var requestedScopes = request.GetScopes().Intersect(AllowedScopes).ToArray();
        var app = string.IsNullOrWhiteSpace(request.ClientId)
            ? null
            : await _applicationManager.FindByClientIdAsync(request.ClientId, HttpContext.RequestAborted);
        var applicationId = app is null ? null : await _applicationManager.GetIdAsync(app, HttpContext.RequestAborted);

        var authorization = await FindValidAuthorizationAsync(user.Id, applicationId, requestedScopes, HttpContext.RequestAborted);
        if (remember && authorization is null && !string.IsNullOrWhiteSpace(applicationId))
        {
            authorization = await _authorizationManager.CreateAsync(
                identity: new ClaimsIdentity(),
                subject: user.Id.ToString(),
                client: applicationId,
                type: OpenIddictConstants.AuthorizationTypes.Permanent,
                scopes: requestedScopes.ToImmutableArray(),
                cancellationToken: HttpContext.RequestAborted);
        }

        var policy = await _tokenPolicyService.GetEffectivePolicyAsync(HttpContext.RequestAborted);
        var principal = await CreatePrincipalAsync(user, requestedScopes, policy, refreshAbsoluteExpiry: null);
        if (authorization is not null)
        {
            principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization, HttpContext.RequestAborted));
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            _logger.LogWarning("Token request missing OpenIddict request data.");
            return BadRequest(CreateErrorResponse(OpenIddictConstants.Errors.InvalidRequest, L("Oidc.Token.InvalidRequest", "The token request is invalid.")));
        }

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType() && !request.IsClientCredentialsGrantType() && !request.IsPasswordGrantType())
        {
            _logger.LogWarning("Unsupported grant type for client {ClientId}.", request.ClientId);
            var supportedGrantTypes = _authFeatures.EnablePasswordGrant
                ? "Only authorization_code, refresh_token, client_credentials, and password grants are supported."
                : "Only authorization_code, refresh_token, and client_credentials grants are supported.";
            return BadRequest(CreateErrorResponse(
                OpenIddictConstants.Errors.UnsupportedGrantType,
                supportedGrantTypes));
        }

        if (request.IsPasswordGrantType() && !_authFeatures.EnablePasswordGrant)
        {
            return BadRequest(CreateErrorResponse(OpenIddictConstants.Errors.UnsupportedGrantType, "The password grant is disabled."));
        }

        var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            _logger.LogWarning("Token request authentication failed for client {ClientId}.", request.ClientId);
            return Forbid();
        }

        var clientId = request.ClientId ?? authenticateResult.Principal.GetClaim(OpenIddictConstants.Claims.ClientId);
        if (!await _clientStateService.IsClientEnabledAsync(clientId, HttpContext.RequestAborted))
        {
            _logger.LogInformation("Token request rejected because client {ClientId} is disabled.", clientId);
            return Forbid(CreateClientDisabledProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var tokenPolicy = await _policyEnforcer.ValidateTokenAsync(request, HttpContext.RequestAborted);
        if (!tokenPolicy.Allowed)
        {
            await _incidentService.LogIncidentAsync(
                "oidc_client_rule_violation",
                "medium",
                null,
                clientId,
                new { clientId, ruleCode = tokenPolicy.RuleCode, path = HttpContext.Request.Path, traceId = HttpContext.TraceIdentifier },
                cancellationToken: HttpContext.RequestAborted);

            return Forbid(new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictConstants.Parameters.Error] = tokenPolicy.Error,
                [OpenIddictConstants.Parameters.ErrorDescription] = tokenPolicy.Description
            }), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsClientCredentialsGrantType())
        {
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            identity.AddClaim(OpenIddictConstants.Claims.Subject, clientId ?? request.ClientId ?? string.Empty);
            var machinePrincipal = new ClaimsPrincipal(identity);
            machinePrincipal.SetScopes(request.GetScopes().Intersect(AllowedScopes));
            machinePrincipal.SetResources(AuthServerScopeRegistry.ApiResources);
            await _loginHistoryService.RecordAsync(null, null, true, null, HttpContext, clientId, HttpContext.RequestAborted);
            return SignIn(machinePrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordGrantAsync(request, clientId);
        }

        var user = await _userManager.GetUserAsync(authenticateResult.Principal);
        if (user is null || !user.IsActive || user.IsAnonymized)
        {
            _logger.LogWarning("Token request rejected because user is missing or inactive.");
            return Forbid(CreateUserDisabledProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var policy = await _tokenPolicyService.GetEffectivePolicyAsync(HttpContext.RequestAborted);

        if (request.IsRefreshTokenGrantType()
            && policy.RefreshRotationEnabled
            && policy.RefreshReuseDetectionEnabled)
        {
            var reuseResult = await _refreshTokenReuseDetection.TryConsumeAsync(
                authenticateResult.Principal,
                policy.RefreshReuseLeewaySeconds,
                HttpContext.RequestAborted);

            if (reuseResult == RefreshTokenReuseResult.Reused)
            {
                await _incidentService.LogIncidentAsync(
                    "refresh_token_reuse",
                    "high",
                    user.Id,
                    clientId,
                    new { clientId, userId = user.Id },
                    cancellationToken: HttpContext.RequestAborted);

                await _refreshTokenReuseRemediation.RevokeSubjectTokensAsync(
                    authenticateResult.Principal,
                    clientId,
                    HttpContext.RequestAborted);
                return Forbid(CreateInvalidGrant(L("Oidc.Token.RefreshReused", "The refresh token has already been used.")),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
        }

        var refreshAbsoluteExpiry = request.IsRefreshTokenGrantType()
            ? TokenPolicyApplier.GetAbsoluteExpiry(authenticateResult.Principal)
            : null;

        if (refreshAbsoluteExpiry is not null && refreshAbsoluteExpiry <= DateTimeOffset.UtcNow)
        {
            return Forbid(CreateInvalidGrant(L("Oidc.Token.RefreshExpired", "The refresh token has expired.")),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var principal = await CreatePrincipalAsync(
            user,
            authenticateResult.Principal.GetScopes(),
            policy,
            refreshAbsoluteExpiry: refreshAbsoluteExpiry);

        await _loginHistoryService.RecordAsync(user.Id, user.Email ?? user.UserName, true, null, HttpContext, clientId, HttpContext.RequestAborted);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Userinfo()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            sub = user.Id,
            name = user.UserName,
            email = user.Email
        });
    }

    [HttpGet("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return SignOut(new AuthenticationProperties { RedirectUri = "/" }, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandlePasswordGrantAsync(OpenIddictRequest request, string? clientId)
    {
        if (!_authFeatures.EnablePasswordGrant)
        {
            return BadRequest(CreateErrorResponse(OpenIddictConstants.Errors.UnsupportedGrantType, "The password grant is disabled."));
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Forbid(CreateInvalidGrant("The username/password credentials are invalid."), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var user = await _userManager.FindByNameAsync(request.Username) ?? await _userManager.FindByEmailAsync(request.Username);
        if (user is null)
        {
            return Forbid(CreateInvalidGrant("The username/password credentials are invalid."), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var passwordResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (passwordResult.RequiresTwoFactor)
        {
            return Forbid(CreateInvalidGrant("Password grant is not available for users requiring multi-factor authentication."), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (!passwordResult.Succeeded)
        {
            return Forbid(CreateInvalidGrant("The username/password credentials are invalid."), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (_signInManager.Options.SignIn.RequireConfirmedAccount && !user.EmailConfirmed)
        {
            return Forbid(CreateInvalidGrant("The user account is not confirmed."), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (!user.IsActive || user.IsAnonymized)
        {
            return Forbid(CreateUserDisabledProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var app = string.IsNullOrWhiteSpace(clientId) ? null : await _applicationManager.FindByClientIdAsync(clientId, HttpContext.RequestAborted);
        var applicationId = app is null ? null : await _applicationManager.GetIdAsync(app, HttpContext.RequestAborted);
        var state = string.IsNullOrWhiteSpace(applicationId)
            ? null
            : await dbContext.ClientStates.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicationId == applicationId, HttpContext.RequestAborted);

        var policySnapshot = ClientPolicySnapshot.From(state?.OverridesJson);
        var requestedScopes = request.GetScopes().ToArray();
        if (requestedScopes.Except(policySnapshot.AllowedScopesForPasswordGrant, StringComparer.OrdinalIgnoreCase).Any())
        {
            return Forbid(new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictConstants.Parameters.Error] = OpenIddictConstants.Errors.InvalidScope,
                [OpenIddictConstants.Parameters.ErrorDescription] = "The requested scopes are not allowed for password grant."
            }), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        principal.SetScopes(requestedScopes.Intersect(AllowedScopes));
        principal.SetResources(AuthServerScopeRegistry.ApiResources);

        var policy = await _tokenPolicyService.GetEffectivePolicyAsync(HttpContext.RequestAborted);
        _tokenPolicyApplier.Apply(principal, policy, DateTimeOffset.UtcNow, absoluteExpiryOverride: null);

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(ClaimDestinations.GetDestinations(claim, principal));
        }

        await _loginHistoryService.RecordAsync(user.Id, user.Email ?? user.UserName, true, null, HttpContext, clientId, HttpContext.RequestAborted);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(
        ApplicationUser user,
        IEnumerable<string> requestedScopes,
        TokenPolicySnapshot policy,
        DateTimeOffset? refreshAbsoluteExpiry)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        identity.AddClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        identity.AddClaim(OpenIddictConstants.Claims.Name, user.UserName ?? string.Empty);

        if (!string.IsNullOrEmpty(user.Email))
        {
            identity.AddClaim(OpenIddictConstants.Claims.Email, user.Email);
        }

        var permissions = await _permissionService.GetPermissionsForUserAsync(user.Id, HttpContext.RequestAborted);
        foreach (var permission in permissions)
        {
            identity.AddClaim(AuthConstants.ClaimTypes.Permission, permission);
        }

        var principal = new ClaimsPrincipal(identity);

        var scopes = requestedScopes.Intersect(AllowedScopes);
        principal.SetScopes(scopes);
        principal.SetResources(AuthServerScopeRegistry.ApiResources);

        _tokenPolicyApplier.Apply(principal, policy, DateTimeOffset.UtcNow, refreshAbsoluteExpiry);

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(ClaimDestinations.GetDestinations(claim, principal));
        }
        return await Task.FromResult(principal);
    }


    private async Task<object?> FindValidAuthorizationAsync(Guid userId, string? applicationId, IReadOnlyCollection<string> scopes, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return null;
        }

        await foreach (var authorization in _authorizationManager.FindAsync(
                           subject: userId.ToString(),
                           client: applicationId,
                           status: OpenIddictConstants.Statuses.Valid,
                           type: OpenIddictConstants.AuthorizationTypes.Permanent,
                           scopes: scopes.ToImmutableArray(),
                           cancellationToken: cancellationToken))
        {
            return authorization;
        }

        return null;
    }

    private ContentResult RenderConsentPage(string clientName, IReadOnlyCollection<string> scopes)
    {
        var tokenSet = _antiforgery.GetAndStoreTokens(HttpContext);
        var requestPath = Request.PathBase + Request.Path + Request.QueryString;
        var encodedAction = HtmlEncoder.Default.Encode(requestPath);
        var encodedClientName = HtmlEncoder.Default.Encode(clientName);
        var token = HtmlEncoder.Default.Encode(tokenSet.RequestToken ?? string.Empty);

        var scopeList = new StringBuilder();
        foreach (var scope in scopes.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            var encodedScope = HtmlEncoder.Default.Encode(scope);
            var encodedDisplayName = HtmlEncoder.Default.Encode(GetScopeDisplayName(scope));
            var encodedDescription = HtmlEncoder.Default.Encode(GetScopeDescription(scope));
            scopeList.Append($"<li><strong>{encodedDisplayName}</strong><br /><span style=\"opacity:0.9\">{encodedDescription}</span><br /><small style=\"opacity:0.7\">{encodedScope}</small></li>");
        }

        var title = HtmlEncoder.Default.Encode(L("Oidc.Consent.Title", "Consent required"));
        var requestText = HtmlEncoder.Default.Encode(L("Oidc.Consent.ClientRequest", "is requesting access to:"));
        var rememberText = HtmlEncoder.Default.Encode(L("Oidc.Consent.Remember", "Remember my choice"));
        var allowText = HtmlEncoder.Default.Encode(L("Oidc.Consent.Allow", "Allow"));
        var denyText = HtmlEncoder.Default.Encode(L("Oidc.Consent.Deny", "Deny"));

        var html = $"""
<!DOCTYPE html>
<html lang="en">
<head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>{title}</title></head>
<body style="font-family:Segoe UI,sans-serif;background:#0f172a;color:#e2e8f0;padding:2rem;">
  <main style="max-width:640px;margin:0 auto;background:#111827;border-radius:12px;padding:1.5rem;">
    <h1 style="margin-top:0">{title}</h1>
    <p><strong>{encodedClientName}</strong> {requestText}</p>
    <ul>{scopeList}</ul>
    <form method="post" action="{encodedAction}">
      <input type="hidden" name="__RequestVerificationToken" value="{token}" />
      <label><input type="checkbox" name="remember" value="true" /> {rememberText}</label>
      <div style="margin-top:1rem;display:flex;gap:0.75rem;">
        <button type="submit" name="decision" value="allow">{allowText}</button>
        <button type="submit" name="decision" value="deny">{denyText}</button>
      </div>
    </form>
  </main>
</body>
</html>
""";

        return Content(html, "text/html");
    }

    private static ISet<string> GetPromptValues(OpenIddictRequest request)
    {
        var prompt = request.GetParameter(OpenIddictConstants.Parameters.Prompt).ToString();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return prompt
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private string GetScopeDisplayName(string scope)
    {
        var key = scope.ToLowerInvariant() switch
        {
            OpenIddictConstants.Scopes.OpenId => "Oidc.Scope.OpenId.Name",
            OpenIddictConstants.Scopes.Profile => "Oidc.Scope.Profile.Name",
            OpenIddictConstants.Scopes.Email => "Oidc.Scope.Email.Name",
            OpenIddictConstants.Scopes.OfflineAccess => "Oidc.Scope.OfflineAccess.Name",
            "api" => "Oidc.Scope.Api.Name",
            "roles" => "Oidc.Scope.Roles.Name",
            _ => "Oidc.Scope.Generic.Name"
        };

        return L(key, scope);
    }

    private string GetScopeDescription(string scope)
    {
        var key = scope.ToLowerInvariant() switch
        {
            OpenIddictConstants.Scopes.OpenId => "Oidc.Scope.OpenId.Description",
            OpenIddictConstants.Scopes.Profile => "Oidc.Scope.Profile.Description",
            OpenIddictConstants.Scopes.Email => "Oidc.Scope.Email.Description",
            OpenIddictConstants.Scopes.OfflineAccess => "Oidc.Scope.OfflineAccess.Description",
            "api" => "Oidc.Scope.Api.Description",
            "roles" => "Oidc.Scope.Roles.Description",
            _ => "Oidc.Scope.Generic.Description"
        };

        return L(key, "Allows the application to access this scope.");
    }

    private string L(string key, string fallback)
    {
        var value = _localizer[key];
        return value.ResourceNotFound ? fallback : value.Value;
    }

    private static AuthenticationProperties CreateClientDisabledProperties()
    {
        return new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictConstants.Parameters.Error] = OpenIddictConstants.Errors.InvalidClient,
            [OpenIddictConstants.Parameters.ErrorDescription] = "The client application is disabled."
        });
    }

    private static AuthenticationProperties CreateUserDisabledProperties()
    {
        return new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictConstants.Parameters.Error] = OpenIddictConstants.Errors.AccessDenied,
            [OpenIddictConstants.Parameters.ErrorDescription] = "The user account is disabled."
        });
    }

    private static OpenIddictResponse CreateErrorResponse(string error, string description)
    {
        return new OpenIddictResponse
        {
            Error = error,
            ErrorDescription = description
        };
    }

    private static AuthenticationProperties CreateInvalidGrant(string description)
    {
        return new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictConstants.Parameters.Error] = OpenIddictConstants.Errors.InvalidGrant,
            [OpenIddictConstants.Parameters.ErrorDescription] = description
        });
    }

}
