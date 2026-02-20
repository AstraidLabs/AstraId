using AuthServer.Services.Admin.Models;
using AuthServer.Services.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;
/// <summary>
/// Exposes HTTP endpoints for admin data protection.
/// </summary>

[ApiController]
[Route("admin/api/security/dataprotection")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminDataProtectionController : ControllerBase
{
    private readonly DataProtectionStatusService _statusService;

    public AdminDataProtectionController(DataProtectionStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpGet("status")]
    public ActionResult<AdminDataProtectionStatus> GetStatus()
    {
        var status = _statusService.GetStatus();
        return Ok(new AdminDataProtectionStatus(
            status.KeysPersisted,
            status.KeyPath,
            status.ReadOnly,
            status.KeyCount,
            status.LatestKeyActivationUtc,
            status.LatestKeyExpirationUtc));
    }
}
