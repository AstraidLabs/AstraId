using AuthServer.Services.Admin.Models;
using AuthServer.Services.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;
/// <summary>
/// Exposes HTTP endpoints for admin encryption status.
/// </summary>

[ApiController]
[Route("admin/api/security/encryption")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminEncryptionStatusController : ControllerBase
{
    private readonly EncryptionKeyStatusService _statusService;

    public AdminEncryptionStatusController(EncryptionKeyStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpGet("status")]
    public ActionResult<AdminEncryptionKeyStatus> GetStatus()
    {
        var status = _statusService.GetStatus();
        return Ok(new AdminEncryptionKeyStatus(
            status.Enabled,
            status.Source,
            status.Thumbprint,
            status.Subject,
            status.NotBeforeUtc,
            status.NotAfterUtc));
    }
}
