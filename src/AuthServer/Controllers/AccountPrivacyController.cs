using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services;
using AuthServer.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers;

[ApiController]
[Route("account")]
[Authorize]
public sealed class AccountPrivacyController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly PrivacyGovernanceService _privacyService;
    private readonly LoginHistoryService _loginHistoryService;
    private readonly UserSessionRevocationService _sessionRevocationService;

    public AccountPrivacyController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, PrivacyGovernanceService privacyService, LoginHistoryService loginHistoryService, UserSessionRevocationService sessionRevocationService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _privacyService = privacyService;
        _loginHistoryService = loginHistoryService;
        _sessionRevocationService = sessionRevocationService;
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized(new ProblemDetails { Title = "Not signed in." }.ApplyDefaults(HttpContext));

        var logins = await _loginHistoryService.GetRecentForUserAsync(user.Id, 200, cancellationToken);
        var payload = new
        {
            user = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.EmailConfirmed,
                user.PhoneNumber,
                user.TwoFactorEnabled,
                user.IsActive,
                user.IsAnonymized
            },
            roles = await _userManager.GetRolesAsync(user),
            loginHistory = logins.Select(x => new { x.TimestampUtc, x.Success, x.FailureReasonCode, x.ClientId, x.Ip, x.UserAgent, x.TraceId }),
            deletionRequests = await _dbContext.DeletionRequests.Where(d => d.UserId == user.Id).OrderByDescending(d => d.RequestedUtc).ToListAsync(cancellationToken)
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"astraid-export-{user.Id:N}.json".Replace(" ", string.Empty));
    }

    [HttpGet("security/login-history")]
    public async Task<IActionResult> LoginHistory([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var items = await _loginHistoryService.GetRecentForUserAsync(user.Id, take, cancellationToken);
        return Ok(items.Select(x => new LoginHistoryResponse(x.Id, x.TimestampUtc, x.Success, x.FailureReasonCode, x.ClientId, x.Ip, x.UserAgent, x.TraceId)));
    }

    [HttpPost("deletion/request")]
    public async Task<IActionResult> RequestDeletion([FromBody] Dictionary<string, string?>? body, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var reason = body is not null && body.TryGetValue("reason", out var r) ? r : null;
        var request = await _privacyService.CreateDeletionRequestAsync(user.Id, reason, cancellationToken);
        return Ok(new DeletionRequestResponse(request.Id, request.RequestedUtc, request.Status.ToString(), request.CooldownUntilUtc, request.ExecutedUtc, request.CancelUtc, request.Reason));
    }

    [HttpPost("deletion/cancel")]
    public async Task<IActionResult> CancelDeletion(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var ok = await _privacyService.CancelDeletionRequestAsync(user.Id, cancellationToken);
        if (!ok)
        {
            return BadRequest(new ProblemDetails { Title = "Deletion request cannot be cancelled after cooldown window." }.ApplyDefaults(HttpContext));
        }

        return Ok(new AuthResponse(true, null, null, "Deletion request cancelled."));
    }

    [HttpPost("sessions/revoke-all")]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        await _sessionRevocationService.RevokeAllForUserAsync(user.Id, "account.sessions.revoke-all", HttpContext, null, cancellationToken);
        return Ok(new AuthResponse(true, null, null, "All sessions revoked."));
    }
}
