using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using AuthServer.Authorization;
using AuthServer.Data;
using AuthServer.Services;
using AuthServer.Services.Governance;
using AuthServer.Services.Tokens;
using AuthServer.Services.Security;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Microsoft.AspNetCore;

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
    private readonly ILogger<AuthorizationController> _logger;

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
        ILogger<AuthorizationController> logger)
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
        _logger = logger;
    }

    [HttpGet("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            _logger.LogWarning("Authorization request missing OpenIddict request data.");
            return BadRequest(CreateErrorResponse(OpenIddictConstants.Errors.InvalidRequest, "The authorization request is invalid."));
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

        var policy = await _tokenPolicyService.GetEffectivePolicyAsync(HttpContext.RequestAborted);

        var principal = await CreatePrincipalAsync(
            user,
            request.GetScopes(),
            policy,
            refreshAbsoluteExpiry: null);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            _logger.LogWarning("Token request missing OpenIddict request data.");
            return BadRequest(CreateErrorResponse(OpenIddictConstants.Errors.InvalidRequest, "The token request is invalid."));
        }

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType() && !request.IsClientCredentialsGrantType())
        {
            _logger.LogWarning("Unsupported grant type for client {ClientId}.", request.ClientId);
            return BadRequest(CreateErrorResponse(
                OpenIddictConstants.Errors.UnsupportedGrantType,
                "Only authorization_code, refresh_token, and client_credentials grants are supported."));
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
                return Forbid(CreateInvalidGrant("The refresh token has already been used."),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
        }

        var refreshAbsoluteExpiry = request.IsRefreshTokenGrantType()
            ? TokenPolicyApplier.GetAbsoluteExpiry(authenticateResult.Principal)
            : null;

        if (refreshAbsoluteExpiry is not null && refreshAbsoluteExpiry <= DateTimeOffset.UtcNow)
        {
            return Forbid(CreateInvalidGrant("The refresh token has expired."),
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
