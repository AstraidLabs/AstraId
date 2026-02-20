using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;
/// <summary>
/// Exposes HTTP endpoints for admin ping.
/// </summary>

[ApiController]
[Route("admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminPingController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { status = "ok" });
    }
}
