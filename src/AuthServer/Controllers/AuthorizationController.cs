using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using AuthServer.Data;
using AuthServer.Services;
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
    private static readonly HashSet<string> AllowedScopes =
    [
        AuthConstants.Scopes.OpenId,
        AuthConstants.Scopes.Profile,
        AuthConstants.Scopes.Email,
        AuthConstants.Scopes.OfflineAccess,
        "api"
    ];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPermissionService _permissionService;
    private readonly UiUrlBuilder _uiUrlBuilder;
    private readonly IClientStateService _clientStateService;

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPermissionService permissionService,
        UiUrlBuilder uiUrlBuilder,
        IClientStateService clientStateService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _permissionService = permissionService;
        _uiUrlBuilder = uiUrlBuilder;
        _clientStateService = clientStateService;
    }

    [HttpGet("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return BadRequest();
        }

        if (!await _clientStateService.IsClientEnabledAsync(request.ClientId, HttpContext.RequestAborted))
        {
            return Forbid(CreateClientDisabledProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            var redirectUri = Request.PathBase + Request.Path + Request.QueryString;
            return Redirect(_uiUrlBuilder.BuildLoginUrl(redirectUri));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Forbid();
        }

        var principal = await CreatePrincipalAsync(user, request.GetScopes());
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return BadRequest();
        }

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            return BadRequest();
        }

        var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            return Forbid();
        }

        var clientId = request.ClientId ?? authenticateResult.Principal.GetClaim(OpenIddictConstants.Claims.ClientId);
        if (!await _clientStateService.IsClientEnabledAsync(clientId, HttpContext.RequestAborted))
        {
            return Forbid(CreateClientDisabledProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var user = await _userManager.GetUserAsync(authenticateResult.Principal);
        if (user is null)
        {
            return Forbid();
        }

        var principal = await CreatePrincipalAsync(user, authenticateResult.Principal.GetScopes());
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

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(ApplicationUser user, IEnumerable<string> requestedScopes)
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
        principal.SetResources("api");

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return await Task.FromResult(principal);
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        return claim.Type switch
        {
            OpenIddictConstants.Claims.Name when principal.HasScope(AuthConstants.Scopes.Profile) =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Email when principal.HasScope(AuthConstants.Scopes.Email) =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Subject =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            AuthConstants.ClaimTypes.Permission =>
                [OpenIddictConstants.Destinations.AccessToken],
            _ =>
                [OpenIddictConstants.Destinations.AccessToken]
        };
    }

    private static AuthenticationProperties CreateClientDisabledProperties()
    {
        return new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
            [OpenIddictConstants.Properties.ErrorDescription] = "The client application is disabled."
        });
    }
}
