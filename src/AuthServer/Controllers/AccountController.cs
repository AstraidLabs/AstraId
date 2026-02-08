using System.Globalization;
using System.Text;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services;
using AuthServer.Services.Notifications;
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
        NotificationService notificationService)
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
    }

    [HttpPost("password/change")]
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

        return Ok(new AuthResponse(true, null, null, "Password updated."));
    }

    [HttpPost("email/change-request")]
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

        return Ok(new AuthResponse(true, _uiUrlBuilder.BuildLoginUrl(string.Empty), null, "Email updated."));
    }

    [HttpPost("sessions/revoke-others")]
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

        return Ok(new AuthResponse(true, null, null, "Other sessions were signed out."));
    }

    [HttpGet("security/overview")]
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

    private bool IsRateLimited(string action, out int retryAfterSeconds)
    {
        return _rateLimiter.IsLimited(HttpContext, action, AccountMaxAttempts, AccountWindow, out retryAfterSeconds);
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
