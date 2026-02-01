using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionService _permissionService;
    private readonly ReturnUrlValidator _returnUrlValidator;
    private readonly UiUrlBuilder _uiUrlBuilder;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IPermissionService permissionService,
        ReturnUrlValidator returnUrlValidator,
        UiUrlBuilder uiUrlBuilder)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _permissionService = permissionService;
        _returnUrlValidator = returnUrlValidator;
        _uiUrlBuilder = uiUrlBuilder;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmailOrUsername) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponse(false, null, "Zadejte e-mail/uživatelské jméno a heslo."));
        }

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl) && !_returnUrlValidator.IsValidReturnUrl(request.ReturnUrl))
        {
            return BadRequest(new AuthResponse(false, null, "Neplatný returnUrl."));
        }

        var user = await _userManager.FindByEmailAsync(request.EmailOrUsername)
                   ?? await _userManager.FindByNameAsync(request.EmailOrUsername);

        if (user is null)
        {
            return BadRequest(new AuthResponse(false, null, "Neplatné přihlašovací údaje."));
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, request.Password, false, false);
        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponse(false, null, "Neplatné přihlašovací údaje."));
        }

        var redirectTo = string.IsNullOrWhiteSpace(request.ReturnUrl)
            ? _uiUrlBuilder.BuildHomeUrl()
            : request.ReturnUrl;

        return Ok(new AuthResponse(true, redirectTo, null));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return BadRequest(new AuthResponse(false, null, "Vyplňte všechny povinné údaje."));
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest(new AuthResponse(false, null, "Hesla se neshodují."));
        }

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl) && !_returnUrlValidator.IsValidReturnUrl(request.ReturnUrl))
        {
            return BadRequest(new AuthResponse(false, null, "Neplatný returnUrl."));
        }

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return BadRequest(new AuthResponse(false, null, "Uživatel s tímto e-mailem již existuje."));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var message = string.Join(" ", createResult.Errors.Select(error => error.Description));
            return BadRequest(new AuthResponse(false, null, message));
        }

        await _signInManager.SignInAsync(user, false);

        var redirectTo = string.IsNullOrWhiteSpace(request.ReturnUrl)
            ? _uiUrlBuilder.BuildHomeUrl()
            : request.ReturnUrl;

        return Ok(new AuthResponse(true, redirectTo, null));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("session")]
    [AllowAnonymous]
    public async Task<IActionResult> Session()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Ok(new AuthSessionResponse(false, null, null, null, Array.Empty<string>(), Array.Empty<string>()));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Ok(new AuthSessionResponse(false, null, null, null, Array.Empty<string>(), Array.Empty<string>()));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionService.GetPermissionsForUserAsync(user.Id, HttpContext.RequestAborted);

        return Ok(new AuthSessionResponse(
            true,
            user.Id.ToString(),
            user.Email,
            user.UserName,
            roles,
            permissions));
    }
}
