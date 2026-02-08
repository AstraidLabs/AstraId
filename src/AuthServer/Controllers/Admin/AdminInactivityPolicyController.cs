using System.Security.Claims;
using AuthServer.Data;
using AuthServer.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("admin/api/security")]
public sealed class AdminInactivityPolicyController : ControllerBase
{
    private readonly InactivityPolicyService _service;

    public AdminInactivityPolicyController(InactivityPolicyService service)
    {
        _service = service;
    }

    [HttpGet("inactivity-policy")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!HasSystemAdmin()) return Forbid();
        return Ok(await _service.GetAsync(cancellationToken));
    }

    [HttpPut("inactivity-policy")]
    public async Task<IActionResult> Put([FromBody] InactivityPolicy policy, CancellationToken cancellationToken)
    {
        if (!HasSystemAdmin()) return Forbid();
        if (policy.WarningAfterDays < 0 || policy.DeactivateAfterDays < 0 || policy.DeleteAfterDays < 0)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["days"] = ["Day values must be greater than or equal to zero."]
            }) { Title = "Invalid inactivity policy." }.ApplyDefaults(HttpContext));
        }

        if (policy.WarningAfterDays > policy.DeactivateAfterDays || policy.DeactivateAfterDays > policy.DeleteAfterDays)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["ordering"] = ["WarningAfterDays <= DeactivateAfterDays <= DeleteAfterDays must hold."]
            }) { Title = "Invalid inactivity policy." }.ApplyDefaults(HttpContext));
        }

        var actorUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : null;
        var updated = await _service.UpdateAsync(policy, actorUserId, cancellationToken);
        return Ok(updated);
    }

    private bool HasSystemAdmin()
        => User.HasClaim(Company.Auth.Contracts.AuthConstants.ClaimTypes.Permission, "system.admin");
}
