using System.Security.Claims;
using AuthServer.Data;
using AuthServer.Services.Security;
using AuthServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/security")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminUserLifecycleController : ControllerBase
{
    private readonly UserLifecycleService _service;
    private readonly ApplicationDbContext _dbContext;

    public AdminUserLifecycleController(UserLifecycleService service, ApplicationDbContext dbContext)
    {
        _service = service;
        _dbContext = dbContext;
    }

    [HttpGet("user-lifecycle-policy")]
    public async Task<IActionResult> GetPolicy(CancellationToken cancellationToken)
    {
        if (!HasSystemAdmin()) return Forbid();
        var policy = await _service.GetPolicyAsync(cancellationToken);
        return Ok(policy);
    }

    [HttpPut("user-lifecycle-policy")]
    public async Task<IActionResult> UpdatePolicy([FromBody] UserLifecyclePolicy policy, CancellationToken cancellationToken)
    {
        if (!HasSystemAdmin()) return Forbid();
        var errors = ValidatePolicy(policy);
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors)
            {
                Title = "Invalid user lifecycle policy."
            }.ApplyDefaults(HttpContext));
        }

        var updated = await _service.UpdatePolicyAsync(policy, GetActorUserId(), cancellationToken);
        return Ok(updated);
    }

    [HttpGet("user-lifecycle/preview")]
    public async Task<IActionResult> Preview([FromQuery] int days, CancellationToken cancellationToken)
    {
        if (!HasSystemAdmin()) return Forbid();
        if (days < 0)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["days"] = ["Days must be greater than or equal to zero."]
            }) { Title = "Invalid preview request." }.ApplyDefaults(HttpContext));
        }

        var result = await _service.PreviewAsync(days, cancellationToken);
        return Ok(new { wouldDeactivate = result.WouldDeactivate, wouldAnonymize = result.WouldAnonymize, wouldHardDelete = result.WouldHardDelete });
    }

    [HttpPost("users/{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        if (!HasSystemAdmin()) return Forbid();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return NotFound(new ProblemDetails { Title = "User not found." }.ApplyDefaults(HttpContext));
        await _service.DeactivateAsync(user, GetActorUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("users/{id:guid}/anonymize")]
    public async Task<IActionResult> Anonymize(Guid id, CancellationToken cancellationToken)
    {
        if (!HasSystemAdmin()) return Forbid();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return NotFound(new ProblemDetails { Title = "User not found." }.ApplyDefaults(HttpContext));
        await _service.AnonymizeAsync(user, GetActorUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("users/{id:guid}/hard-delete")]
    public async Task<IActionResult> HardDelete(Guid id, [FromQuery] bool confirm, CancellationToken cancellationToken)
    {
        if (!HasSystemAdmin()) return Forbid();
        if (!confirm)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["confirm"] = ["Hard delete requires confirm=true."]
            }) { Title = "Confirmation required." }.ApplyDefaults(HttpContext));
        }

        var policy = await _service.GetPolicyAsync(cancellationToken);
        if (!policy.HardDeleteEnabled)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["hardDeleteEnabled"] = ["Hard delete is disabled by policy."]
            }) { Title = "Hard delete disabled." }.ApplyDefaults(HttpContext));
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return NotFound(new ProblemDetails { Title = "User not found." }.ApplyDefaults(HttpContext));
        await _service.HardDeleteAsync(user, GetActorUserId(), cancellationToken);
        return NoContent();
    }

    private bool HasSystemAdmin()
        => User.HasClaim(Company.Auth.Contracts.AuthConstants.ClaimTypes.Permission, "system.admin");

    private Guid? GetActorUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static Dictionary<string, string[]> ValidatePolicy(UserLifecyclePolicy policy)
    {
        var errors = new Dictionary<string, string[]>();
        if (policy.DeactivateAfterDays < 0) errors["deactivateAfterDays"] = ["DeactivateAfterDays must be greater than or equal to zero."];
        if (policy.DeleteAfterDays < 0) errors["deleteAfterDays"] = ["DeleteAfterDays must be greater than or equal to zero."];
        if (policy.DeleteAfterDays < policy.DeactivateAfterDays) errors["deleteAfterDays"] = ["DeleteAfterDays must be greater than or equal to DeactivateAfterDays."];
        if (policy.HardDeleteAfterDays is < 0) errors["hardDeleteAfterDays"] = ["HardDeleteAfterDays must be greater than or equal to zero."];
        if (policy.HardDeleteAfterDays.HasValue && policy.HardDeleteAfterDays.Value < policy.DeleteAfterDays) errors["hardDeleteAfterDays"] = ["HardDeleteAfterDays must be greater than or equal to DeleteAfterDays."];
        if (policy.WarnBeforeLogoutMinutes < 0 || policy.WarnBeforeLogoutMinutes > 1440) errors["warnBeforeLogoutMinutes"] = ["WarnBeforeLogoutMinutes must be between 0 and 1440."];
        if (policy.IdleLogoutMinutes < 1 || policy.IdleLogoutMinutes > 43200) errors["idleLogoutMinutes"] = ["IdleLogoutMinutes must be between 1 and 43200."];
        return errors;
    }
}
