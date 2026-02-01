using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

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
    private readonly IEmailSender _emailSender;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IPermissionService permissionService,
        ReturnUrlValidator returnUrlValidator,
        UiUrlBuilder uiUrlBuilder,
        IEmailSender emailSender)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _permissionService = permissionService;
        _returnUrlValidator = returnUrlValidator;
        _uiUrlBuilder = uiUrlBuilder;
        _emailSender = emailSender;
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

        if (!user.EmailConfirmed)
        {
            return BadRequest(new AuthResponse(false, null, "Účet není aktivní. Zkontrolujte potvrzovací e-mail."));
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
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var message = string.Join(" ", createResult.Errors.Select(error => error.Description));
            return BadRequest(new AuthResponse(false, null, message));
        }

        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = EncodeToken(confirmationToken);
        var activationLink = _uiUrlBuilder.BuildActivationUrl(user.Email!, encodedToken);
        var (subject, htmlBody, textBody) = EmailTemplates.BuildActivationEmail(activationLink);
        await _emailSender.SendAsync(
            new EmailMessage(user.Email!, user.UserName, subject, htmlBody, textBody),
            HttpContext.RequestAborted);

        var redirectTo = string.IsNullOrWhiteSpace(request.ReturnUrl)
            ? _uiUrlBuilder.BuildLoginUrl(string.Empty)
            : _uiUrlBuilder.BuildLoginUrl(request.ReturnUrl);

        return Ok(new AuthResponse(true, redirectTo, null));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new AuthResponse(false, null, "Zadejte e-mail."));
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = EncodeToken(resetToken);
            var resetLink = _uiUrlBuilder.BuildResetPasswordUrl(user.Email!, encodedToken);
            var (subject, htmlBody, textBody) = EmailTemplates.BuildResetPasswordEmail(resetLink);
            await _emailSender.SendAsync(
                new EmailMessage(user.Email!, user.UserName, subject, htmlBody, textBody),
                HttpContext.RequestAborted);
        }

        return Ok(new AuthResponse(true, null, null));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.NewPassword) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return BadRequest(new AuthResponse(false, null, "Vyplňte všechny povinné údaje."));
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest(new AuthResponse(false, null, "Hesla se neshodují."));
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Ok(new AuthResponse(true, null, null));
        }

        var decodedToken = DecodeToken(request.Token);
        if (decodedToken is null)
        {
            return BadRequest(new AuthResponse(false, null, "Neplatný nebo expirovaný token."));
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(error => error.Description));
            return BadRequest(new AuthResponse(false, null, message));
        }

        return Ok(new AuthResponse(true, null, null));
    }

    [HttpPost("resend-activation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendActivation([FromBody] ResendActivationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new AuthResponse(false, null, "Zadejte e-mail."));
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null && !user.EmailConfirmed)
        {
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = EncodeToken(confirmationToken);
            var activationLink = _uiUrlBuilder.BuildActivationUrl(user.Email!, encodedToken);
            var (subject, htmlBody, textBody) = EmailTemplates.BuildActivationEmail(activationLink);
            await _emailSender.SendAsync(
                new EmailMessage(user.Email!, user.UserName, subject, htmlBody, textBody),
                HttpContext.RequestAborted);
        }

        return Ok(new AuthResponse(true, null, null));
    }

    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<IActionResult> ActivateAccount([FromBody] ActivateAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new AuthResponse(false, null, "Vyplňte všechny povinné údaje."));
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return BadRequest(new AuthResponse(false, null, "Neplatný nebo expirovaný token."));
        }

        var decodedToken = DecodeToken(request.Token);
        if (decodedToken is null)
        {
            return BadRequest(new AuthResponse(false, null, "Neplatný nebo expirovaný token."));
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(error => error.Description));
            return BadRequest(new AuthResponse(false, null, message));
        }

        return Ok(new AuthResponse(true, null, null));
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
            roles.ToList(),
            permissions));
    }

    private static string EncodeToken(string token)
    {
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    private static string? DecodeToken(string encodedToken)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
