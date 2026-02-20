using System.Security.Claims;
using AuthServer.Data;
using AuthServer.Services;
using AuthServer.Services.Security;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;
/// <summary>
/// Exposes HTTP endpoints for admin privacy policy.
/// </summary>

[ApiController]
[Route("admin/api/security")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminPrivacyPolicyController : ControllerBase
{
    private readonly PrivacyGovernanceService _privacyService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IPermissionService _permissionService;

    public AdminPrivacyPolicyController(
        PrivacyGovernanceService privacyService,
        ApplicationDbContext dbContext,
        IPermissionService permissionService)
    {
        _privacyService = privacyService;
        _dbContext = dbContext;
        _permissionService = permissionService;
    }

    [HttpGet("privacy-policy")]
    public async Task<IActionResult> GetPolicy(CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AuthConstants.Permissions.Gdpr.Read, cancellationToken)) return Forbid();
        var policy = await _privacyService.GetPolicyAsync(cancellationToken);
        return Ok(policy);
    }

    [HttpPut("privacy-policy")]
    public async Task<IActionResult> UpdatePolicy([FromBody] PrivacyPolicy request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AuthConstants.Permissions.Gdpr.RetentionManage, cancellationToken)) return Forbid();
        request.LoginHistoryRetentionDays = Math.Max(1, request.LoginHistoryRetentionDays);
        request.ErrorLogRetentionDays = Math.Max(1, request.ErrorLogRetentionDays);
        request.AuditLogRetentionDays = Math.Max(1, request.AuditLogRetentionDays);
        request.TokenRetentionDays = Math.Max(1, request.TokenRetentionDays);
        request.DeletionCooldownDays = Math.Max(0, request.DeletionCooldownDays);

        var updated = await _privacyService.UpdatePolicyAsync(request, GetActorUserId(), cancellationToken);
        return Ok(updated);
    }

    [HttpGet("deletion-requests")]
    public async Task<IActionResult> List([FromQuery] string? q, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AuthConstants.Permissions.Gdpr.Read, cancellationToken)) return Forbid();

        var query = _dbContext.DeletionRequests.AsNoTracking()
            .Join(_dbContext.Users, d => d.UserId, u => u.Id, (d, u) => new { d, u.Email, u.UserName });

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => (x.Email ?? "").Contains(term) || (x.UserName ?? "").Contains(term));
        }

        var items = await query.OrderByDescending(x => x.d.RequestedUtc).Take(200).ToListAsync(cancellationToken);
        return Ok(items.Select(x => new { x.d.Id, x.d.UserId, x.Email, x.UserName, status = x.d.Status.ToString(), x.d.RequestedUtc, x.d.CooldownUntilUtc, x.d.ExecutedUtc, x.d.CancelUtc, x.d.Reason }));
    }

    [HttpPost("deletion-requests/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AuthConstants.Permissions.Gdpr.Erase, cancellationToken)) return Forbid();
        var request = await _dbContext.DeletionRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null) return NotFound(new ProblemDetails { Title = "Deletion request not found." }.ApplyDefaults(HttpContext));
        request.Status = DeletionRequestStatus.Approved;
        request.ApprovedBy = GetActorUserId();
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _privacyService.AddAuditAsync(GetActorUserId(), "privacy.deletion.approved", "DeletionRequest", request.Id.ToString(), null, cancellationToken);
        return NoContent();
    }

    [HttpPost("deletion-requests/{id:guid}/execute")]
    public async Task<IActionResult> Execute(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AuthConstants.Permissions.Gdpr.Erase, cancellationToken)) return Forbid();
        var request = await _dbContext.DeletionRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null) return NotFound(new ProblemDetails { Title = "Deletion request not found." }.ApplyDefaults(HttpContext));
        await _privacyService.ExecuteErasureAsync(request, GetActorUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("deletion-requests/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AuthConstants.Permissions.Gdpr.Erase, cancellationToken)) return Forbid();
        var request = await _dbContext.DeletionRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null) return NotFound(new ProblemDetails { Title = "Deletion request not found." }.ApplyDefaults(HttpContext));
        request.Status = DeletionRequestStatus.Cancelled;
        request.CancelUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _privacyService.AddAuditAsync(GetActorUserId(), "privacy.deletion.admin-cancelled", "DeletionRequest", request.Id.ToString(), null, cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken)
    {
        var userId = GetActorUserId();
        if (!userId.HasValue)
        {
            return false;
        }

        var userPermissions = await _permissionService.GetPermissionsForUserAsync(userId.Value, cancellationToken);
        return userPermissions.Contains(AuthConstants.Permissions.SystemAdmin) || userPermissions.Contains(permission);
    }

    private Guid? GetActorUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
