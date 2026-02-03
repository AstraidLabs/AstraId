using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/roles")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminRolesController : ControllerBase
{
    private readonly IAdminRoleService _roleService;

    public AdminRolesController(IAdminRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminRoleListItem>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetRolesAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminRoleDetail>> GetRole(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        var permissionIds = await _roleService.GetRolePermissionIdsAsync(id, cancellationToken);
        return Ok(new AdminRoleDetail(role.Id, role.Name ?? string.Empty, role.Name == "Admin", permissionIds));
    }

    [HttpPost]
    public async Task<ActionResult<AdminRoleListItem>> CreateRole(
        [FromBody] AdminRoleCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Invalid role name.",
                Detail = "Role name is required."
            });
        }

        var trimmedName = request.Name.Trim();
        var result = await _roleService.CreateRoleAsync(trimmedName);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Role creation failed.",
                Detail = string.Join("; ", result.Errors.Select(error => error.Description))
            });
        }

        var roles = await _roleService.GetRolesAsync(cancellationToken);
        var created = roles.FirstOrDefault(role => string.Equals(role.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
        return created is null ? Ok() : CreatedAtAction(nameof(GetRole), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        var result = await _roleService.DeleteRoleAsync(role);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Role deletion failed.",
                Detail = string.Join("; ", result.Errors.Select(error => error.Description))
            });
        }

        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(
        Guid id,
        [FromBody] AdminRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var role = await _roleService.GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        await _roleService.SetRolePermissionsAsync(id, request.PermissionIds, cancellationToken);
        return NoContent();
    }
}
