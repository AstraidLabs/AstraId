using AuthServer.Data;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/roles")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminRolesController : ControllerBase
{
    private readonly IAdminRoleService _roleService;
    private readonly ApplicationDbContext _dbContext;

    public AdminRolesController(IAdminRoleService roleService, ApplicationDbContext dbContext)
    {
        _roleService = roleService;
        _dbContext = dbContext;
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
            return NotFound(new ProblemDetails { Title = "Role not found." });
        }

        var permissionIds = await _roleService.GetRolePermissionIdsAsync(id, cancellationToken);
        return Ok(new AdminRoleDetail(role.Id, role.Name ?? string.Empty, role.Name == "Admin", permissionIds));
    }

    [HttpGet("{id:guid}/usage")]
    public async Task<ActionResult<AdminRoleUsage>> GetRoleUsage(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound(new ProblemDetails { Title = "Role not found." });
        }

        var userCount = await _dbContext.UserRoles.CountAsync(item => item.RoleId == id, cancellationToken);
        return Ok(new AdminRoleUsage(userCount));
    }

    [HttpPost]
    public async Task<ActionResult<AdminRoleListItem>> CreateRole(
        [FromBody] AdminRoleCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["name"] = ["Role name is required."]
            })
            {
                Title = "Invalid role name.",
                Detail = "Role name is required."
            });
        }

        var trimmedName = request.Name.Trim();
        var result = await _roleService.CreateRoleAsync(trimmedName);
        if (!result.Succeeded)
        {
            var messages = result.Errors.Select(error => error.Description).Where(message => !string.IsNullOrWhiteSpace(message)).ToArray();
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["name"] = messages.Length == 0 ? ["Role creation failed."] : messages
            })
            {
                Title = "Role creation failed.",
                Detail = string.Join("; ", messages)
            });
        }

        var roles = await _roleService.GetRolesAsync(cancellationToken);
        var created = roles.FirstOrDefault(role => string.Equals(role.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
        return created is null ? Ok() : CreatedAtAction(nameof(GetRole), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] AdminRoleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["name"] = ["Role name is required."]
            })
            {
                Title = "Invalid role name.",
                Detail = "Role name is required."
            });
        }

        var role = await _roleService.GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound(new ProblemDetails { Title = "Role not found." });
        }

        var trimmedName = request.Name.Trim();
        var result = await _roleService.UpdateRoleAsync(role, trimmedName);
        if (!result.Succeeded)
        {
            var messages = result.Errors.Select(error => error.Description).Where(message => !string.IsNullOrWhiteSpace(message)).ToArray();
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["name"] = messages.Length == 0 ? ["Role update failed."] : messages
            })
            {
                Title = "Role update failed.",
                Detail = string.Join("; ", messages)
            });
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound(new ProblemDetails { Title = "Role not found." });
        }

        var userCount = await _dbContext.UserRoles.CountAsync(item => item.RoleId == id, cancellationToken);
        if (userCount > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["role"] = ["Role is assigned to one or more users. Remove assignments before deleting."]
            })
            {
                Title = "Role is in use.",
                Detail = "Role is assigned to one or more users. Remove assignments before deleting."
            });
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
            return NotFound(new ProblemDetails { Title = "Role not found." });
        }

        await _roleService.SetRolePermissionsAsync(id, request.PermissionIds, cancellationToken);
        return NoContent();
    }
}
