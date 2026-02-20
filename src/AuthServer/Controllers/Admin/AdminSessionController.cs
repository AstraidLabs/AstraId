using System.Security.Claims;
using AuthServer.Data;
using AuthServer.Services;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;
/// <summary>
/// Exposes HTTP endpoints for admin session.
/// </summary>

[ApiController]
[Route("admin/api/me")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminSessionController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionService _permissionService;

    public AdminSessionController(UserManager<ApplicationUser> userManager, IPermissionService permissionService)
    {
        _userManager = userManager;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<ActionResult<AdminSessionInfo>> GetSession(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionService.GetPermissionsForUserAsync(userId, cancellationToken);

        return Ok(new AdminSessionInfo(
            userId,
            user.Email,
            user.UserName,
            roles.ToList(),
            permissions.ToList()));
    }
}
