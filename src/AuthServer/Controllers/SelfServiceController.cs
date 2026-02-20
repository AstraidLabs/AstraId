using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services;
using AuthServer.Services.Notifications;
using AuthServer.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace AuthServer.Controllers;
/// <summary>
/// Exposes HTTP endpoints for self service.
/// </summary>

[ApiController]
[Route("auth/me")]
[Authorize]
public sealed class SelfServiceController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UiUrlBuilder _uiUrlBuilder;
    private readonly IEmailSender _emailSender;
    private readonly UserSessionRevocationService _sessionRevocationService;
    private readonly IUserSecurityEventLogger _eventLogger;
    private readonly UserLifecycleService _lifecycleService;
    private readonly NotificationService _notificationService;

    public SelfServiceController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        UiUrlBuilder uiUrlBuilder,
        IEmailSender emailSender,
        UserSessionRevocationService sessionRevocationService,
        IUserSecurityEventLogger eventLogger,
        UserLifecycleService lifecycleService,
        NotificationService notificationService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _uiUrlBuilder = uiUrlBuilder;
        _emailSender = emailSender;
        _sessionRevocationService = sessionRevocationService;
        _eventLogger = eventLogger;
        _lifecycleService = lifecycleService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMe()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var lastLogin = await HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>()
            .UserSecurityEvents
            .Where(e => e.UserId == user.Id && (e.EventType == "LoginSuccess" || e.EventType == "MfaLoginSuccess"))
            .OrderByDescending(e => e.TimestampUtc)
            .Select(e => (DateTimeOffset?)e.TimestampUtc)
            .FirstOrDefaultAsync(HttpContext.RequestAborted);

        return Ok(new MeSummaryResponse(
            user.Id.ToString(),
            user.Email,
            user.UserName,
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            roles.ToArray(),
            null,
            lastLogin));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordSelfRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.CurrentPassword)) errors["currentPassword"] = ["Current password is required."];
        if (string.IsNullOrWhiteSpace(request.NewPassword)) errors["newPassword"] = ["New password is required."];
        if (string.IsNullOrWhiteSpace(request.ConfirmNewPassword)) errors["confirmNewPassword"] = ["Password confirmation is required."];
        if (!string.IsNullOrWhiteSpace(request.NewPassword)
            && !string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            errors["confirmNewPassword"] = ["Passwords do not match."];
        }

        if (errors.Count > 0) return Validation(errors, "Invalid password change request.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return Validation(new Dictionary<string, string[]>
            {
                ["currentPassword"] = result.Errors.Select(e => e.Description).DefaultIfEmpty("Password change failed.").ToArray()
            }, "Password change failed.");
        }

        await _userManager.UpdateSecurityStampAsync(user);
        await _lifecycleService.TrackPasswordChangeAsync(user.Id, DateTime.UtcNow, HttpContext.RequestAborted);
        await _eventLogger.LogAsync("PasswordChanged", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _notificationService.NotifyPasswordChangedAsync(user, HttpContext.TraceIdentifier, HttpContext.RequestAborted);
        }
        return Ok(new AuthResponse(true, null, null, "Password updated."));
    }

    [HttpPost("change-email/start")]
    public async Task<IActionResult> ChangeEmailStart([FromBody] ChangeEmailStartRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.NewEmail)) errors["newEmail"] = ["New email is required."];
        if (string.IsNullOrWhiteSpace(request.Password)) errors["password"] = ["Password is required."];
        if (errors.Count > 0) return Validation(errors, "Invalid email change request.");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Validation(new Dictionary<string, string[]> { ["password"] = ["Password is incorrect."] }, "Invalid email change request.");
        }

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, request.NewEmail);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = _uiUrlBuilder.BuildChangeEmailUrl(user.Id, request.NewEmail, encodedToken, "/account/security/email");
        var (subject, htmlBody, textBody) = EmailTemplates.BuildChangeEmailEmail(link);
        await _emailSender.SendAsync(new EmailMessage(request.NewEmail, user.UserName, subject, htmlBody, textBody), HttpContext.RequestAborted);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _notificationService.QueueAsync(user.Id, NotificationType.EmailChangeRequestedOld, user.Email!, user.UserName,
                "Email change requested",
                $"<p>A request was made to change your account email to {request.NewEmail}. If this was not you, secure your account.</p>",
                $"A request was made to change your account email to {request.NewEmail}. If this was not you, secure your account.",
                HttpContext.TraceIdentifier, null, null, HttpContext.RequestAborted);
        }

        return Ok(new AuthResponse(true, null, null, "Confirmation sent to your new email."));
    }

    [AllowAnonymous]
    [HttpPost("change-email/confirm")]
    public async Task<IActionResult> ChangeEmailConfirm([FromBody] ConfirmEmailChangeSelfRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) return BadRequest();

        string decoded;
        try { decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token)); }
        catch { return BadRequest(); }

        var result = await _userManager.ChangeEmailAsync(user, request.NewEmail, decoded);
        if (!result.Succeeded) return BadRequest();

        await _userManager.SetUserNameAsync(user, request.NewEmail);
        await _userManager.UpdateSecurityStampAsync(user);
        await _sessionRevocationService.RevokeAllForUserAsync(user.Id, "account.sessions.revoked", HttpContext, new { reason = "email-change" }, HttpContext.RequestAborted);
        await _eventLogger.LogAsync("EmailChanged", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
        await _notificationService.QueueAsync(user.Id, NotificationType.EmailChangedNew, request.NewEmail, user.UserName,
            "Your email was changed",
            "<p>This email address is now associated with your AstraId account.</p>",
            "This email address is now associated with your AstraId account.",
            HttpContext.TraceIdentifier, null, null, HttpContext.RequestAborted);

        return Ok(new AuthResponse(true, null, null, "Email updated."));
    }

    [HttpPost("signout-all")]
    public async Task<IActionResult> SignOutAll()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        await _sessionRevocationService.RevokeAllForUserAsync(user.Id, "account.sessions.revoked", HttpContext, new { reason = "manual" }, HttpContext.RequestAborted);
        await _userManager.UpdateSecurityStampAsync(user);
        await _signInManager.SignOutAsync();
        await _eventLogger.LogAsync("SignOutAllSessions", user.Id, HttpContext, cancellationToken: HttpContext.RequestAborted);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _notificationService.NotifySessionsRevokedAsync(user, "manual", HttpContext.TraceIdentifier, HttpContext.RequestAborted);
        }
        return Ok(new SignOutAllSessionsResponse(true, "All sessions were signed out."));
    }

    [HttpGet("security-events")]
    public async Task<IActionResult> SecurityEvents([FromQuery] int take = 20)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var events = await _eventLogger.GetRecentForUserAsync(user.Id, take, HttpContext.RequestAborted);
        return Ok(events.Select(evt => new UserSecurityEventResponse(
            evt.Id,
            evt.TimestampUtc,
            evt.EventType,
            evt.IpAddress,
            evt.UserAgent,
            evt.ClientId,
            evt.TraceId)));
    }

    private IActionResult Validation(Dictionary<string, string[]> errors, string title)
    {
        var details = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = title,
            Detail = ProblemDetailsDefaults.GetDefaultDetail(StatusCodes.Status422UnprocessableEntity)
        }.ApplyDefaults(HttpContext);

        return UnprocessableEntity(details);
    }
}
