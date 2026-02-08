using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services;
using Company.Auth.Contracts;
using AuthServer.Services.Security;
using AuthServer.Services.Notifications;
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
    private readonly UserSessionRevocationService _sessionRevocationService;
    private readonly string _issuerName;
    private readonly IUserSecurityEventLogger _eventLogger;
    private readonly UserLifecycleService _userLifecycleService;
    private readonly LoginHistoryService _loginHistoryService;
    private readonly NotificationService _notificationService;

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
        UserSessionRevocationService sessionRevocationService,
        IUserSecurityEventLogger eventLogger,
        UserLifecycleService userLifecycleService,
        LoginHistoryService loginHistoryService,
        NotificationService notificationService,
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
        _sessionRevocationService = sessionRevocationService;
        _eventLogger = eventLogger;
        _userLifecycleService = userLifecycleService;
        _loginHistoryService = loginHistoryService;
        _notificationService = notificationService;
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
            return BuildValidationProblem(
                "Invalid sign-in request.",
                new Dictionary<string, string[]>
                {
                    ["emailOrUsername"] = string.IsNullOrWhiteSpace(emailOrUsername)
                        ? ["Email or username is required."]
                        : [],
                    ["password"] = string.IsNullOrWhiteSpace(request.Password)
                        ? ["Password is required."]
                        : []
                });
        }

        var returnUrl = request.ReturnUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(returnUrl) && !_returnUrlValidator.IsValidReturnUrl(returnUrl))
        {
            return BuildValidationProblem(
                "Invalid sign-in request.",
                new Dictionary<string, string[]>
                {
                    ["returnUrl"] = ["The return URL is invalid."]
                });
        }

        var user = await _userManager.FindByEmailAsync(emailOrUsername)
                   ?? await _userManager.FindByNameAsync(emailOrUsername);

        if (user is null)
        {
            await _eventLogger.LogAsync("LoginFailed", null, HttpContext, cancellationToken: HttpContext.RequestAborted);
            await _loginHistoryService.RecordAsync(null, emailOrUsername, false, "unknown_user", HttpContext, null, HttpContext.RequestAborted);
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Sign-in failed",
                "The email/username or password is incorrect.");
        }

        if (!user.IsActive || user.IsAnonymized)
        {
            return BuildAuthProblem(
                StatusCodes.Status403Forbidden,
                "Sign-in unavailable",
                "Sign-in is temporarily unavailable. Please try again later.");
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

        if (result.IsLockedOut)
        {
            return BuildAuthProblem(
                StatusCodes.Status423Locked,
                "Account locked",
                "Your account is temporarily locked due to too many failed attempts. Please try again later or reset your password.");
        }

        if (result.IsNotAllowed)
        {
            if (!user.EmailConfirmed)
            {
                return BuildAuthProblem(
                    StatusCodes.Status403Forbidden,
                    "Email not verified",
                    "Please verify your email address before signing in. If you didn't receive the email, you can request a new verification link.");
            }

            return BuildAuthProblem(
                StatusCodes.Status403Forbidden,
                "Sign-in unavailable",
                "Sign-in is temporarily unavailable. Please try again later.");
        }

        if (!result.Succeeded)
        {
            await _eventLogger.LogAsync("LoginFailed", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
            await _loginHistoryService.RecordAsync(user.Id, emailOrUsername, false, "invalid_password", HttpContext, null, HttpContext.RequestAborted);
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Sign-in failed",
                "The email/username or password is incorrect.");
        }

        await _eventLogger.LogAsync("LoginSuccess", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
        await _loginHistoryService.RecordAsync(user.Id, emailOrUsername, true, null, HttpContext, null, HttpContext.RequestAborted);
        await _userLifecycleService.TrackLoginAsync(user.Id, DateTime.UtcNow, HttpContext.RequestAborted);
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
            return BuildValidationProblem(
                "Invalid authentication request.",
                new Dictionary<string, string[]>
                {
                    ["mfaToken"] = string.IsNullOrWhiteSpace(request.MfaToken)
                        ? ["MFA token is required."]
                        : [],
                    ["code"] = string.IsNullOrWhiteSpace(request.Code)
                        ? ["MFA code is required."]
                        : []
                });
        }

        if (!_mfaChallengeStore.TryConsume(request.MfaToken, out var challenge))
        {
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Invalid code",
                "The code you entered is invalid. Please try again.");
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null || user.Id != challenge.UserId)
        {
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Invalid code",
                "The code you entered is invalid. Please try again.");
        }

        var code = NormalizeCode(request.Code);
        Microsoft.AspNetCore.Identity.SignInResult result;
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
            if (result.IsNotAllowed)
            {
                await _eventLogger.LogAsync("MfaLoginFailed", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
            return BuildAuthProblem(
                    StatusCodes.Status403Forbidden,
                    "Two-factor authentication unavailable",
                    "Two-factor authentication is currently disabled. Please contact support if you need assistance.");
            }

            await _eventLogger.LogAsync("MfaLoginFailed", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Invalid code",
                "The code you entered is invalid. Please try again.");
        }

        await _eventLogger.LogAsync("MfaLoginSuccess", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
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
            return BuildAuthProblem(
                StatusCodes.Status409Conflict,
                "Two-factor authentication already enabled",
                "Two-factor authentication is already enabled for this account.");
        }

        var resetResult = await _userManager.ResetAuthenticatorKeyAsync(user);
        if (!resetResult.Succeeded)
        {
            return BuildAuthProblem(
                StatusCodes.Status500InternalServerError,
                "Two-factor authentication unavailable",
                "Two-factor authentication is currently disabled. Please contact support if you need assistance.");
        }

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            return BuildAuthProblem(
                StatusCodes.Status500InternalServerError,
                "Two-factor authentication unavailable",
                "Two-factor authentication is currently disabled. Please contact support if you need assistance.");
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
            return BuildAuthProblem(
                StatusCodes.Status409Conflict,
                "Two-factor authentication already enabled",
                "Two-factor authentication is already enabled for this account.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BuildValidationProblem(
                "Invalid verification code.",
                new Dictionary<string, string[]>
                {
                    ["code"] = ["Verification code is required."]
                });
        }

        var code = NormalizeCode(request.Code);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!isValid)
        {
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Invalid code",
                "The code you entered is invalid. Please try again.");
        }

        var enableResult = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!enableResult.Succeeded)
        {
            return BuildAuthProblem(
                StatusCodes.Status500InternalServerError,
                "Two-factor authentication unavailable",
                "Two-factor authentication is currently disabled. Please contact support if you need assistance.");
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        await _sessionRevocationService.RevokeAllForUserAsync(
            user.Id,
            null,
            HttpContext,
            new { reason = "mfa-enabled" },
            HttpContext.RequestAborted);
        await _signInManager.SignInAsync(user, isPersistent: false);
        await _eventLogger.LogAsync("MfaEnabled", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _notificationService.QueueAsync(user.Id, NotificationType.MfaEnabled, user.Email!, user.UserName,
                "Multi-factor authentication enabled",
                "<p>Multi-factor authentication was enabled on your account.</p>",
                "Multi-factor authentication was enabled on your account.",
                HttpContext.TraceIdentifier, null, null, HttpContext.RequestAborted);
        }
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
            return BuildAuthProblem(
                StatusCodes.Status409Conflict,
                "Two-factor authentication unavailable",
                "Two-factor authentication is currently disabled. Please contact support if you need assistance.");
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        await _sessionRevocationService.RevokeAllForUserAsync(
            user.Id,
            null,
            HttpContext,
            new { reason = "mfa-enabled" },
            HttpContext.RequestAborted);
        await _signInManager.SignInAsync(user, isPersistent: false);
        await _eventLogger.LogAsync("RecoveryCodesRegenerated", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _notificationService.QueueAsync(user.Id, NotificationType.RecoveryCodesRegenerated, user.Email!, user.UserName,
                "Recovery codes regenerated",
                "<p>Your MFA recovery codes were regenerated. Previous codes no longer work.</p>",
                "Your MFA recovery codes were regenerated. Previous codes no longer work.",
                HttpContext.TraceIdentifier, null, null, HttpContext.RequestAborted);
        }
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
            return BuildAuthProblem(
                StatusCodes.Status409Conflict,
                "Two-factor authentication unavailable",
                "Two-factor authentication is currently disabled. Please contact support if you need assistance.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BuildValidationProblem(
                "Invalid verification code.",
                new Dictionary<string, string[]>
                {
                    ["code"] = ["Verification code is required."]
                });
        }

        var code = NormalizeCode(request.Code);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);
        if (!isValid)
        {
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Invalid code",
                "The code you entered is invalid. Please try again.");
        }

        var disableResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disableResult.Succeeded)
        {
            return BuildAuthProblem(
                StatusCodes.Status500InternalServerError,
                "Two-factor authentication unavailable",
                "Two-factor authentication is currently disabled. Please contact support if you need assistance.");
        }

        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);
        await _sessionRevocationService.RevokeAllForUserAsync(
            user.Id,
            null,
            HttpContext,
            new { reason = "mfa-disabled" },
            HttpContext.RequestAborted);
        await _signInManager.SignInAsync(user, isPersistent: false);
        await _eventLogger.LogAsync("MfaDisabled", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _notificationService.QueueAsync(user.Id, NotificationType.MfaDisabled, user.Email!, user.UserName,
                "Multi-factor authentication disabled",
                "<p>Multi-factor authentication was disabled on your account.</p>",
                "Multi-factor authentication was disabled on your account.",
                HttpContext.TraceIdentifier, null, null, HttpContext.RequestAborted);
        }

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
            return BuildValidationProblem(
                "Invalid registration request.",
                new Dictionary<string, string[]>
                {
                    ["email"] = string.IsNullOrWhiteSpace(email) ? ["Email is required."] : [],
                    ["password"] = string.IsNullOrWhiteSpace(request.Password) ? ["Password is required."] : [],
                    ["confirmPassword"] = string.IsNullOrWhiteSpace(request.ConfirmPassword)
                        ? ["Password confirmation is required."]
                        : []
                });
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BuildValidationProblem(
                "Invalid registration request.",
                new Dictionary<string, string[]>
                {
                    ["confirmPassword"] = ["Passwords do not match."]
                });
        }

        var returnUrl = request.ReturnUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(returnUrl) && !_returnUrlValidator.IsValidReturnUrl(returnUrl))
        {
            return BuildValidationProblem(
                "Invalid registration request.",
                new Dictionary<string, string[]>
                {
                    ["returnUrl"] = ["The return URL is invalid."]
                });
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
            var passwordErrors = createResult.Errors
                .Select(error => error.Description)
                .Where(description => !string.IsNullOrWhiteSpace(description))
                .ToArray();
            return BuildValidationProblem(
                "Registration failed",
                new Dictionary<string, string[]>
                {
                    ["password"] = passwordErrors.Length > 0 ? passwordErrors : ["Registration failed."]
                });
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
            return BuildValidationProblem(
                "Invalid password reset request.",
                new Dictionary<string, string[]>
                {
                    ["email"] = ["Email is required."]
                });
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

        return Ok(new AuthResponse(true, null, null, "If an account exists for this email, you’ll receive a password reset link shortly."));
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
            return BuildValidationProblem(
                "Invalid password reset request.",
                new Dictionary<string, string[]>
                {
                    ["email"] = string.IsNullOrWhiteSpace(email) ? ["Email is required."] : [],
                    ["token"] = string.IsNullOrWhiteSpace(request.Token) ? ["Reset token is required."] : [],
                    ["newPassword"] = string.IsNullOrWhiteSpace(request.NewPassword) ? ["New password is required."] : [],
                    ["confirmPassword"] = string.IsNullOrWhiteSpace(request.ConfirmPassword)
                        ? ["Password confirmation is required."]
                        : []
                });
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BuildValidationProblem(
                "Invalid password reset request.",
                new Dictionary<string, string[]>
                {
                    ["confirmPassword"] = ["Passwords do not match."]
                });
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Ok(new AuthResponse(true, null, null));
        }

        var decodedToken = DecodeToken(request.Token);
        if (decodedToken is null)
        {
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Invalid reset link",
                "This password reset link is invalid or has expired. Please request a new one.");
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded)
        {
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Invalid reset link",
                "This password reset link is invalid or has expired. Please request a new one.");
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
            return BuildValidationProblem(
                "Invalid activation request.",
                new Dictionary<string, string[]>
                {
                    ["email"] = ["Email is required."]
                });
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
            return BuildValidationProblem(
                "Invalid activation request.",
                new Dictionary<string, string[]>
                {
                    ["email"] = string.IsNullOrWhiteSpace(email) ? ["Email is required."] : [],
                    ["token"] = string.IsNullOrWhiteSpace(request.Token) ? ["Activation token is required."] : []
                });
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Invalid verification link",
                "This verification link is invalid or has expired. Please request a new one.");
        }

        var decodedToken = DecodeToken(request.Token);
        if (decodedToken is null)
        {
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Invalid verification link",
                "This verification link is invalid or has expired. Please request a new one.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            return BuildAuthProblem(
                StatusCodes.Status400BadRequest,
                "Invalid verification link",
                "This verification link is invalid or has expired. Please request a new one.");
        }

        return Ok(new AuthResponse(true, null, null));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        await _signInManager.SignOutAsync();
        await _eventLogger.LogAsync("Logout", user?.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
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
        if (user is null || !user.IsActive || user.IsAnonymized)
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

        return BuildAuthProblem(
            StatusCodes.Status429TooManyRequests,
            "Too many attempts",
            "Too many attempts. Please try again later.");
    }

    private IActionResult BuildAuthProblem(int statusCode, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        }.ApplyDefaults(HttpContext);

        return StatusCode(statusCode, problem);
    }

    private IActionResult BuildValidationProblem(string title, Dictionary<string, string[]> errors)
    {
        var filtered = errors
            .Where(entry => entry.Value is { Length: > 0 })
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        var details = new ValidationProblemDetails(filtered)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = title,
            Detail = ProblemDetailsDefaults.GetDefaultDetail(StatusCodes.Status422UnprocessableEntity)
        }.ApplyDefaults(HttpContext);

        return UnprocessableEntity(details);
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
