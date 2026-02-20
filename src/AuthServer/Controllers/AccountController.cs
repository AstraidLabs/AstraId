using System.Globalization;
using System.Text;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services;
using AuthServer.Services.Notifications;
using AuthServer.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthServer.Controllers;

[ApiController]
[Route("account")]
[Authorize]
public sealed class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ReturnUrlValidator _returnUrlValidator;
    private readonly UiUrlBuilder _uiUrlBuilder;
    private readonly IEmailSender _emailSender;
    private readonly AuthRateLimiter _rateLimiter;
    private readonly UserSessionRevocationService _sessionRevocationService;
    private readonly ApplicationDbContext _dbContext;
    private readonly NotificationService _notificationService;
    private readonly IStringLocalizer<AuthMessages> _localizer;

    private static readonly TimeSpan AccountWindow = TimeSpan.FromMinutes(5);
    private const int AccountMaxAttempts = 5;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ReturnUrlValidator returnUrlValidator,
        UiUrlBuilder uiUrlBuilder,
        IEmailSender emailSender,
        AuthRateLimiter rateLimiter,
        UserSessionRevocationService sessionRevocationService,
        ApplicationDbContext dbContext,
        NotificationService notificationService,
        IStringLocalizer<AuthMessages> localizer)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _returnUrlValidator = returnUrlValidator;
        _uiUrlBuilder = uiUrlBuilder;
        _emailSender = emailSender;
        _rateLimiter = rateLimiter;
        _sessionRevocationService = sessionRevocationService;
        _dbContext = dbContext;
        _notificationService = notificationService;
        _localizer = localizer;
    }

    [HttpPost("password/change")]
    /// <summary>
    /// Processes a password change request for the authenticated user.
    /// </summary>
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (IsRateLimited("account-password-change", out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var validationErrors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            validationErrors["currentPassword"] = ["Current password is required."];
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            validationErrors["newPassword"] = ["New password is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            validationErrors["confirmPassword"] = ["Password confirmation is required."];
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword)
            && !string.IsNullOrWhiteSpace(request.ConfirmPassword)
            && !string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            validationErrors["confirmPassword"] = ["Passwords do not match."];
        }

        if (validationErrors.Count > 0)
        {
            return BuildValidationProblem("Invalid password change request.", validationErrors);
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = new Dictionary<string, List<string>>();
            foreach (var error in result.Errors)
            {
                var key = error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                    ? "newPassword"
                    : "currentPassword";

                if (!errors.TryGetValue(key, out var list))
                {
                    list = [];
                    errors[key] = list;
                }

                list.Add(error.Description);
            }

            return BuildValidationProblem(
                "Password change failed.",
                errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
        }

        await _userManager.UpdateSecurityStampAsync(user);

        if (request.SignOutOtherSessions)
        {
            await _sessionRevocationService.RevokeAllForUserAsync(
                user.Id,
                "account.sessions.revoked",
                HttpContext,
                new { reason = "password-change" },
                HttpContext.RequestAborted);
            await _signInManager.SignInAsync(user, isPersistent: false);
        }

        await AddAuditAsync(
            user.Id,
            "account.password.changed",
            "User",
            user.Id.ToString(),
            new { signOutOtherSessions = request.SignOutOtherSessions });

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _notificationService.NotifyPasswordChangedAsync(user, HttpContext.TraceIdentifier, HttpContext.RequestAborted);
        }

        return Ok(new AuthResponse(true, null, null, L("Account.Password.Updated", "Password updated.")));
    }

    [HttpPost("email/change-request")]
    /// <summary>
    /// Starts an email change flow by validating credentials and sending a confirmation link.
    /// </summary>
    public async Task<IActionResult> ChangeEmailRequest([FromBody] ChangeEmailRequest request)
    {
        if (IsRateLimited("account-email-change-request", out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var newEmail = request.NewEmail?.Trim();
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(newEmail))
        {
            errors["newEmail"] = ["New email is required."];
        }
        else
        {
            var emailAttribute = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (!emailAttribute.IsValid(newEmail))
            {
                errors["newEmail"] = ["Email is invalid."];
            }
        }

        var returnUrl = request.ReturnUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(returnUrl) && !_returnUrlValidator.IsValidReturnUrl(returnUrl))
        {
            errors["returnUrl"] = ["The return URL is invalid."];
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            errors["currentPassword"] = ["Current password is required."];
        }

        if (errors.Count > 0)
        {
            return BuildValidationProblem("Invalid email change request.", errors);
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!isPasswordValid)
        {
            return BuildValidationProblem(
                "Invalid email change request.",
                new Dictionary<string, string[]>
                {
                    ["currentPassword"] = ["Current password is incorrect."]
                });
        }

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail!);
        var encodedToken = EncodeToken(token);
        var link = _uiUrlBuilder.BuildChangeEmailUrl(user.Id, newEmail!, encodedToken, returnUrl);
        var (subject, htmlBody, textBody) = EmailTemplates.BuildChangeEmailEmail(link);

        await _emailSender.SendAsync(
            new EmailMessage(newEmail!, user.UserName, subject, htmlBody, textBody),
            HttpContext.RequestAborted);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _notificationService.QueueAsync(user.Id, NotificationType.EmailChangeRequestedOld, user.Email!, user.UserName,
                "Email change requested",
                $"<p>A request was made to change your account email to {MaskEmail(newEmail!)}. If this was not you, secure your account.</p>",
                $"A request was made to change your account email to {MaskEmail(newEmail!)}. If this was not you, secure your account.",
                HttpContext.TraceIdentifier, null, null, HttpContext.RequestAborted);
        }

        await _notificationService.QueueAsync(user.Id, NotificationType.EmailChangeRequestedNew, newEmail!, user.UserName,
            "Confirm your new email address",
            "<p>A confirmation link has been sent for this email change request.</p>",
            "A confirmation link has been sent for this email change request.",
            HttpContext.TraceIdentifier, null, null, HttpContext.RequestAborted);

        await AddAuditAsync(
            user.Id,
            "account.email.change.requested",
            "User",
            user.Id.ToString(),
            new { newEmail = MaskEmail(newEmail!) });

        return Ok(new AuthResponse(
            true,
            null,
            null,
            "If the email can be changed, you will receive a confirmation link."));
    }

    [AllowAnonymous]
    [HttpPost("email/change-confirm")]
    /// <summary>
    /// Confirms a pending email change using the token delivered to the new address.
    /// </summary>
    public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest request)
    {
        if (IsRateLimited("account-email-change-confirm", out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        if (string.IsNullOrWhiteSpace(request.NewEmail) || string.IsNullOrWhiteSpace(request.Token))
        {
            return BuildValidationProblem(
                "Invalid email change request.",
                new Dictionary<string, string[]>
                {
                    ["newEmail"] = string.IsNullOrWhiteSpace(request.NewEmail) ? ["New email is required."] : [],
                    ["token"] = string.IsNullOrWhiteSpace(request.Token) ? ["Token is required."] : []
                });
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return BuildAuthProblem(StatusCodes.Status400BadRequest, "Invalid link", "Invalid link");
        }

        var decodedToken = DecodeToken(request.Token);
        if (decodedToken is null)
        {
            return BuildAuthProblem(StatusCodes.Status400BadRequest, "Invalid link", "Invalid link");
        }

        var result = await _userManager.ChangeEmailAsync(user, request.NewEmail, decodedToken);
        if (!result.Succeeded)
        {
            return BuildAuthProblem(StatusCodes.Status400BadRequest, "Invalid link", "Invalid link");
        }

        var userNameResult = await _userManager.SetUserNameAsync(user, request.NewEmail);
        if (!userNameResult.Succeeded)
        {
            return BuildAuthProblem(StatusCodes.Status400BadRequest, "Invalid link", "Invalid link");
        }

        await _userManager.UpdateSecurityStampAsync(user);
        await _sessionRevocationService.RevokeAllForUserAsync(
            user.Id,
            "account.sessions.revoked",
            HttpContext,
            new { reason = "email-change" },
            HttpContext.RequestAborted);

        await AddAuditAsync(
            user.Id,
            "account.email.change.confirmed",
            "User",
            user.Id.ToString(),
            new { newEmail = MaskEmail(request.NewEmail) });

        await _notificationService.QueueAsync(user.Id, NotificationType.EmailChangedNew, request.NewEmail, user.UserName,
            "Your email was changed",
            "<p>This email address is now associated with your AstraId account.</p>",
            "This email address is now associated with your AstraId account.",
            HttpContext.TraceIdentifier, null, null, HttpContext.RequestAborted);

        return Ok(new AuthResponse(true, _uiUrlBuilder.BuildLoginUrl(string.Empty), null, L("Account.Email.Updated", "Email updated.")));
    }

    [HttpPost("sessions/revoke-others")]
    /// <summary>
    /// Revokes active sessions for the current user except the session issuing the request.
    /// </summary>
    public async Task<IActionResult> RevokeOtherSessions([FromBody] RevokeSessionsRequest request)
    {
        if (IsRateLimited("account-sessions-revoke", out var retryAfter))
        {
            return TooManyRequestsResponse(retryAfter);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return BuildValidationProblem(
                "Invalid session revocation request.",
                new Dictionary<string, string[]>
                {
                    ["currentPassword"] = ["Current password is required."]
                });
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!isPasswordValid)
        {
            return BuildValidationProblem(
                "Invalid session revocation request.",
                new Dictionary<string, string[]>
                {
                    ["currentPassword"] = ["Current password is incorrect."]
                });
        }

        await _sessionRevocationService.RevokeAllForUserAsync(
            user.Id,
            "account.sessions.revoked",
            HttpContext,
            new { reason = "manual" },
            HttpContext.RequestAborted);
        await _userManager.UpdateSecurityStampAsync(user);
        await _signInManager.SignInAsync(user, isPersistent: false);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _notificationService.NotifySessionsRevokedAsync(user, "manual", HttpContext.TraceIdentifier, HttpContext.RequestAborted);
        }

        return Ok(new AuthResponse(true, null, null, L("Account.Sessions.Revoked", "Other sessions were signed out.")));
    }

    [HttpGet("security/overview")]
    /// <summary>
    /// Returns a security overview for the authenticated account.
    /// </summary>
    public async Task<IActionResult> GetSecurityOverview()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        var recoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

        return Ok(new SecurityOverviewResponse(
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            recoveryCodesLeft,
            !string.IsNullOrWhiteSpace(key),
            user.Email,
            user.UserName));
    }

    [HttpGet("preferences")]
    /// <summary>
    /// Returns profile preferences for the authenticated user.
    /// </summary>
    public async Task<IActionResult> GetPreferences()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var preferred = string.IsNullOrWhiteSpace(user.PreferredLanguage)
            ? null
            : LanguageTagNormalizer.Normalize(user.PreferredLanguage);
        var effective = preferred ?? CultureInfo.CurrentUICulture.Name;

        return Ok(new LanguagePreferenceResponse(preferred, effective));
    }

    [HttpPut("preferences/language")]
    /// <summary>
    /// Updates the preferred language for the authenticated user.
    /// </summary>
    public async Task<IActionResult> SetPreferredLanguage([FromBody] UpdateLanguagePreferenceRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!LanguageTagNormalizer.TryNormalize(request.Language, out var normalized))
        {
            return BuildValidationProblem(
                L("Common.Validation.InvalidRequest", "Invalid request."),
                new Dictionary<string, string[]>
                {
                    ["language"] = [L("Account.Preferences.Language.Invalid", "Unsupported language selection.")]
                });
        }

        user.PreferredLanguage = normalized;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BuildAuthProblem(
                StatusCodes.Status500InternalServerError,
                L("Common.Error.Title", "Operation failed"),
                L("Common.Error.Detail", "The operation could not be completed."));
        }

        return Ok(new LanguagePreferenceResponse(user.PreferredLanguage, user.PreferredLanguage));
    }

    /// <summary>
    /// Persists an account-level audit entry for the current operation.
    /// </summary>
    private async Task AddAuditAsync(Guid actorUserId, string action, string targetType, string targetId, object payload)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = JsonSerializer.Serialize(new
            {
                payload,
                ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                traceId = HttpContext.TraceIdentifier
            })
        });

        await _dbContext.SaveChangesAsync(HttpContext.RequestAborted);
    }

    /// <summary>
    /// Checks whether the current user has exceeded the configured rate limit for an action.
    /// </summary>
    private bool IsRateLimited(string action, out int retryAfterSeconds)
    {
        return _rateLimiter.IsLimited(HttpContext, action, AccountMaxAttempts, AccountWindow, out retryAfterSeconds);
    }

    /// <summary>
    /// Builds a standard too-many-requests response with retry metadata.
    /// </summary>
    private IActionResult TooManyRequestsResponse(int retryAfterSeconds)
    {
        if (retryAfterSeconds > 0)
        {
            Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        return BuildAuthProblem(
            StatusCodes.Status429TooManyRequests,
            L("Auth.RateLimit.TooManyAttempts", "Too many attempts"),
            L("Auth.RateLimit.TooManyAttempts.Detail", "Too many attempts. Please try again later."));
    }

    /// <summary>
    /// Creates a problem-details response for authentication and authorization errors.
    /// </summary>
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

    /// <summary>
    /// Creates a validation problem response for field-level request errors.
    /// </summary>
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

    /// <summary>
    /// Resolves a localized message and falls back to the provided text when missing.
    /// </summary>
    private string L(string key, string fallback)
    {
        var value = _localizer[key];
        return value.ResourceNotFound ? fallback : value.Value;
    }

    /// <summary>
    /// Encodes an identity token value so it can be safely used in URLs.
    /// </summary>
    private static string EncodeToken(string token)
    {
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    /// <summary>
    /// Decodes a URL-safe token back into its original representation.
    /// </summary>
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

    /// <summary>
    /// Masks the local-part of an email address for audit and notification messages.
    /// </summary>
    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        return $"{email[0]}***{email[(atIndex - 1)..]}";
    }
}
