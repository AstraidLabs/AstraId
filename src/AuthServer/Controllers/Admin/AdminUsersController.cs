using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _userService;
    private readonly IAdminRoleService _roleService;

    public AdminUsersController(IAdminUserService userService, IAdminRoleService roleService)
    {
        _userService = userService;
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminUserListItem>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetUsersAsync(search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminUserDetail>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userService.GetUserRolesAsync(user);
        return Ok(new AdminUserDetail(
            user.Id,
            user.Email,
            user.UserName,
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
            roles));
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> UpdateRoles(
        Guid id,
        [FromBody] AdminUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var result = await _userService.SetUserRolesAsync(user, request.Roles);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Failed to update user roles.",
                Detail = string.Join("; ", result.Errors.Select(error => error.Description))
            });
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> SetLock(
        Guid id,
        [FromBody] AdminUserLockRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        await _userService.SetLockoutAsync(user, request.IsLockedOut, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] AdminUserResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Invalid password.",
                Detail = "New password is required."
            });
        }

        var result = await _userService.ResetPasswordAsync(user, request.NewPassword);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Failed to reset password.",
                Detail = string.Join("; ", result.Errors.Select(error => error.Description))
            });
        }

        return NoContent();
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<AdminRoleListItem>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetRolesAsync(cancellationToken);
        return Ok(roles);
    }
}
