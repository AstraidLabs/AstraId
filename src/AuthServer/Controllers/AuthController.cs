using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;

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
    private readonly AuthRateLimiter _rateLimiter;
    private readonly MfaChallengeStore _mfaChallengeStore;
    private readonly string _issuerName;

    private static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RegistrationWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResetWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MfaWindow = TimeSpan.FromMinutes(5);
    private const int LoginMaxAttempts = 5;
    private const int MfaMaxAttempts = 5;
    private const int RegistrationMaxAttempts = 3;
    private const int ResetMaxAttempts = 3;
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IPermissionService permissionService,
        ReturnUrlValidator returnUrlValidator,
        UiUrlBuilder uiUrlBuilder,
        IEmailSender emailSender,
        AuthRateLimiter rateLimiter,
        MfaChallengeStore mfaChallengeStore,
        IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _permissionService = permissionService;
        _returnUrlValidator = returnUrlValidator;
        _uiUrlBuilder = uiUrlBuilder;
        _emailSender = emailSender;
        _rateLimiter = rateLimiter;
        _mfaChallengeStore = mfaChallengeStore;
        _issuerName = ResolveIssuerName(configuration);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (IsRateLimited("login", LoginMaxAttempts, LoginWindow, out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        var emailOrUsername = request.EmailOrUsername?.Trim();
        if (string.IsNullOrWhiteSpace(emailOrUsername) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new LoginResponse(false, null, "Zadejte e-mail/uživatelské jméno a heslo."));
        }

        var returnUrl = request.ReturnUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(returnUrl) && !_returnUrlValidator.IsValidReturnUrl(returnUrl))
        {
            return BadRequest(new LoginResponse(false, null, "Neplatný returnUrl."));
        }

        var user = await _userManager.FindByEmailAsync(emailOrUsername)
                   ?? await _userManager.FindByNameAsync(emailOrUsername);

        if (user is null)
        {
            return BadRequest(new LoginResponse(false, null, "Neplatné přihlašovací údaje."));
        }

        if (!user.IsActive)
        {
            return BadRequest(new LoginResponse(false, null, "Účet je deaktivovaný."));
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, request.Password, false, true);
        var redirectTo = string.IsNullOrWhiteSpace(returnUrl)
            ? _uiUrlBuilder.BuildHomeUrl()
            : returnUrl;

        if (result.RequiresTwoFactor)
        {
            var token = _mfaChallengeStore.Create(user.Id, redirectTo);
            return Ok(new LoginResponse(false, redirectTo, null, true, token));
        }

        if (!result.Succeeded)
        {
            return BadRequest(new LoginResponse(false, null, "Neplatné přihlašovací údaje."));
        }

        return Ok(new LoginResponse(true, redirectTo, null));
    }

    [HttpPost("login/mfa")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginMfa([FromBody] MfaLoginRequest request)
    {
        if (IsRateLimited("login-mfa", MfaMaxAttempts, MfaWindow, out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        if (string.IsNullOrWhiteSpace(request.MfaToken) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new AuthResponse(false, null, "Zadejte MFA kód."));
        }

        if (!_mfaChallengeStore.TryConsume(request.MfaToken, out var challenge))
        {
            return BadRequest(new AuthResponse(false, null, "MFA výzva je neplatná nebo expirovaná."));
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null || user.Id != challenge.UserId)
        {
            return BadRequest(new AuthResponse(false, null, "MFA výzva je neplatná nebo expirovaná."));
        }

        var code = NormalizeCode(request.Code);
        SignInResult result;
        if (request.UseRecoveryCode)
        {
            result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);
        }
        else
        {
            result = await _signInManager.TwoFactorAuthenticatorSignInAsync(code, false, request.RememberMachine);
        }

        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponse(false, null, "Neplatný ověřovací kód."));
        }

        var redirectTo = string.IsNullOrWhiteSpace(challenge.ReturnUrl)
            ? _uiUrlBuilder.BuildHomeUrl()
            : challenge.ReturnUrl;

        return Ok(new AuthResponse(true, redirectTo, null));
    }

    [HttpGet("mfa/status")]
    [Authorize]
    public async Task<IActionResult> GetMfaStatus()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        var recoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

        return Ok(new MfaStatusResponse(user.TwoFactorEnabled, !string.IsNullOrWhiteSpace(key), recoveryCodesLeft));
    }

    [HttpPost("mfa/setup/start")]
    [Authorize]
    public async Task<IActionResult> StartMfaSetup()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (user.TwoFactorEnabled)
        {
            return BadRequest(new AuthResponse(false, null, "MFA je již aktivní. Nejprve ji vypněte."));
        }

        var resetResult = await _userManager.ResetAuthenticatorKeyAsync(user);
        if (!resetResult.Succeeded)
        {
            return BadRequest(new AuthResponse(false, null, "Nepodařilo se připravit MFA."));
        }

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new AuthResponse(false, null, "Nepodařilo se připravit MFA."));
        }

        var sharedKey = FormatKey(key);
        var otpauth = BuildOtpAuthUri(user.Email ?? user.UserName ?? "user", key);
        var qrSvg = GenerateQrCodeSvg(otpauth);
        return Ok(new MfaSetupResponse(sharedKey, otpauth, qrSvg));
    }

    [HttpPost("mfa/setup/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmMfaSetup([FromBody] MfaConfirmRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (user.TwoFactorEnabled)
        {
            return BadRequest(new AuthResponse(false, null, "MFA je již aktivní."));
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new AuthResponse(false, null, "Zadejte ověřovací kód."));
        }

        var code = NormalizeCode(request.Code);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!isValid)
        {
            return BadRequest(new AuthResponse(false, null, "Neplatný ověřovací kód."));
        }

        var enableResult = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!enableResult.Succeeded)
        {
            return BadRequest(new AuthResponse(false, null, "Nepodařilo se aktivovat MFA."));
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return Ok(new MfaRecoveryCodesResponse(recoveryCodes.ToArray()));
    }

    [HttpPost("mfa/recovery-codes/regenerate")]
    [Authorize]
    public async Task<IActionResult> RegenerateRecoveryCodes()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!user.TwoFactorEnabled)
        {
            return BadRequest(new AuthResponse(false, null, "MFA není aktivní."));
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return Ok(new MfaRecoveryCodesResponse(recoveryCodes.ToArray()));
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableMfa([FromBody] MfaDisableRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!user.TwoFactorEnabled)
        {
            return BadRequest(new AuthResponse(false, null, "MFA není aktivní."));
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new AuthResponse(false, null, "Zadejte ověřovací kód."));
        }

        var code = NormalizeCode(request.Code);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);
        if (!isValid)
        {
            return BadRequest(new AuthResponse(false, null, "Neplatný ověřovací kód."));
        }

        var disableResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disableResult.Succeeded)
        {
            return BadRequest(new AuthResponse(false, null, "Nepodařilo se vypnout MFA."));
        }

        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _userManager.ReplaceRecoveryCodesAsync(user, Array.Empty<string>());

        return Ok(new AuthResponse(true, null, null));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (IsRateLimited("register", RegistrationMaxAttempts, RegistrationWindow, out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return BadRequest(new AuthResponse(false, null, "Vyplňte všechny povinné údaje."));
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest(new AuthResponse(false, null, "Hesla se neshodují."));
        }

        var returnUrl = request.ReturnUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(returnUrl) && !_returnUrlValidator.IsValidReturnUrl(returnUrl))
        {
            return BadRequest(new AuthResponse(false, null, "Neplatný returnUrl."));
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!existing.EmailConfirmed)
            {
                var existingConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(existing);
                var existingEncodedToken = EncodeToken(existingConfirmationToken);
                var existingActivationLink = _uiUrlBuilder.BuildActivationUrl(existing.Email!, existingEncodedToken);
                var (existingSubject, existingHtmlBody, existingTextBody) =
                    EmailTemplates.BuildActivationEmail(existingActivationLink);
                await _emailSender.SendAsync(
                    new EmailMessage(
                        existing.Email!,
                        existing.UserName,
                        existingSubject,
                        existingHtmlBody,
                        existingTextBody),
                    HttpContext.RequestAborted);
            }

            return Ok(BuildRegistrationResponse(returnUrl));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new AuthResponse(false, null, "Registrace se nezdařila."));
        }

        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = EncodeToken(confirmationToken);
        var activationLink = _uiUrlBuilder.BuildActivationUrl(user.Email!, encodedToken);
        var (subject, htmlBody, textBody) = EmailTemplates.BuildActivationEmail(activationLink);
        await _emailSender.SendAsync(
            new EmailMessage(user.Email!, user.UserName, subject, htmlBody, textBody),
            HttpContext.RequestAborted);

        return Ok(BuildRegistrationResponse(returnUrl));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (IsRateLimited("forgot-password", ResetMaxAttempts, ResetWindow, out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new AuthResponse(false, null, "Zadejte e-mail."));
        }

        var user = await _userManager.FindByEmailAsync(email);
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
        if (IsRateLimited("reset-password", ResetMaxAttempts, ResetWindow, out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) ||
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

        var user = await _userManager.FindByEmailAsync(email);
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
            return BadRequest(new AuthResponse(false, null, "Obnovení hesla se nezdařilo."));
        }

        return Ok(new AuthResponse(true, null, null));
    }

    [HttpPost("resend-activation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendActivation([FromBody] ResendActivationRequest request)
    {
        if (IsRateLimited("resend-activation", ResetMaxAttempts, ResetWindow, out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new AuthResponse(false, null, "Zadejte e-mail."));
        }

        var user = await _userManager.FindByEmailAsync(email);
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
        if (IsRateLimited("activate", ResetMaxAttempts, ResetWindow, out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new AuthResponse(false, null, "Vyplňte všechny povinné údaje."));
        }

        var user = await _userManager.FindByEmailAsync(email);
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
            return BadRequest(new AuthResponse(false, null, "Neplatný nebo expirovaný token."));
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

    private bool IsRateLimited(string action, int maxAttempts, TimeSpan window, out int retryAfterSeconds)
    {
        return _rateLimiter.IsLimited(HttpContext, action, maxAttempts, window, out retryAfterSeconds);
    }

    private IActionResult TooManyRequestsResponse(int retryAfterSeconds)
    {
        if (retryAfterSeconds > 0)
        {
            Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        return StatusCode(
            StatusCodes.Status429TooManyRequests,
            new AuthResponse(false, null, "Příliš mnoho pokusů. Zkuste to prosím později."));
    }

    private AuthResponse BuildRegistrationResponse(string? returnUrl)
    {
        var redirectTo = string.IsNullOrWhiteSpace(returnUrl)
            ? _uiUrlBuilder.BuildLoginUrl(string.Empty)
            : _uiUrlBuilder.BuildLoginUrl(returnUrl);

        return new AuthResponse(true, redirectTo, null);
    }

    private static string NormalizeCode(string code)
    {
        return code.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
    }

    private static string FormatKey(string key)
    {
        var result = new StringBuilder();
        var current = 0;
        while (current + 4 < key.Length)
        {
            result.Append(key.AsSpan(current, 4)).Append(' ');
            current += 4;
        }

        if (current < key.Length)
        {
            result.Append(key.AsSpan(current));
        }

        return result.ToString().ToLowerInvariant();
    }

    private string BuildOtpAuthUri(string email, string unformattedKey)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            UrlEncoder.Default.Encode(_issuerName),
            UrlEncoder.Default.Encode(email),
            unformattedKey);
    }

    private static string ResolveIssuerName(IConfiguration configuration)
    {
        var issuer = configuration["AuthServer:Issuer"] ?? AuthConstants.DefaultIssuer;
        if (Uri.TryCreate(issuer, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host;
        }

        return "AstraId";
    }

    private static string GenerateQrCodeSvg(string payload)
    {
        var generator = new QRCoder.QRCodeGenerator();
        var data = generator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCoder.SvgQRCode(data);
        var svg = qrCode.GetGraphic(4);
        var svgIndex = svg.IndexOf("<svg", StringComparison.OrdinalIgnoreCase);
        return svgIndex >= 0 ? svg[svgIndex..] : svg;
    }
}
