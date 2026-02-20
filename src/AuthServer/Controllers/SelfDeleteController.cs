using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;
/// <summary>
/// Exposes HTTP endpoints for self delete.
/// </summary>

[ApiController]
[Route("auth/self")]
[Authorize]
public sealed class SelfDeleteController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserSessionRevocationService _revocationService;

    public SelfDeleteController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        UserSessionRevocationService revocationService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _revocationService = revocationService;
    }

    [HttpPost("delete-request")]
    public async Task<IActionResult> DeleteRequest()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        user.RequestedDeletionUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _revocationService.RevokeAllForUserAsync(user.Id, "user.self-delete.requested", HttpContext, new { user.RequestedDeletionUtc }, HttpContext.RequestAborted);
        await _signInManager.SignOutAsync();

        return Ok(new AuthResponse(true, null, null, "Your deletion request has been scheduled. Your active sessions were revoked."));
    }
}
