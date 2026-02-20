using AuthServer.Data;
using AuthServer.Services;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using AuthServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;
/// <summary>
/// Exposes HTTP endpoints for admin roles.
/// </summary>

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
            return NotFound(new ProblemDetails { Title = "Role not found." }.ApplyDefaults(HttpContext));
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
            return NotFound(new ProblemDetails { Title = "Role not found." }.ApplyDefaults(HttpContext));
        }

        var userCount = await _dbContext.UserRoles.CountAsync(item => item.RoleId == id, cancellationToken);
        return Ok(new AdminRoleUsage(userCount));
    }

    [HttpPost]
    public async Task<ActionResult<AdminRoleListItem>> CreateRole(
        [FromBody] AdminRoleCreateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = AdminValidation.ValidateRoleName(request.Name);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid role name.").ApplyDefaults(HttpContext));
        }

        var trimmedName = request.Name!.Trim();
        if (await _dbContext.Roles.AnyAsync(
                role => role.Name != null && role.Name.ToLower() == trimmedName.ToLower(),
                cancellationToken))
        {
            validation.AddFieldError("name", $"Role name '{trimmedName}' is already in use.");
            return ValidationProblem(validation.ToProblemDetails("Role already exists.").ApplyDefaults(HttpContext));
        }

        var result = await _roleService.CreateRoleAsync(trimmedName);
        if (!result.Succeeded)
        {
            var messages = result.Errors.Select(error => error.Description).Where(message => !string.IsNullOrWhiteSpace(message)).ToArray();
            if (messages.Length == 0)
            {
                validation.AddGeneralError("Role creation failed.");
            }
            else
            {
                foreach (var message in messages)
                {
                    validation.AddFieldError("name", message);
                }
            }

            return ValidationProblem(validation.ToProblemDetails("Role creation failed.").ApplyDefaults(HttpContext));
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
        var validation = AdminValidation.ValidateRoleName(request.Name);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid role name.").ApplyDefaults(HttpContext));
        }

        var role = await _roleService.GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound(new ProblemDetails { Title = "Role not found." }.ApplyDefaults(HttpContext));
        }

        var trimmedName = request.Name!.Trim();
        if (await _dbContext.Roles.AnyAsync(
                item => item.Id != id && item.Name != null && item.Name.ToLower() == trimmedName.ToLower(),
                cancellationToken))
        {
            validation.AddFieldError("name", $"Role name '{trimmedName}' is already in use.");
            return ValidationProblem(validation.ToProblemDetails("Role already exists.").ApplyDefaults(HttpContext));
        }

        var result = await _roleService.UpdateRoleAsync(role, trimmedName);
        if (!result.Succeeded)
        {
            var messages = result.Errors.Select(error => error.Description).Where(message => !string.IsNullOrWhiteSpace(message)).ToArray();
            if (messages.Length == 0)
            {
                validation.AddGeneralError("Role update failed.");
            }
            else
            {
                foreach (var message in messages)
                {
                    validation.AddFieldError("name", message);
                }
            }

            return ValidationProblem(validation.ToProblemDetails("Role update failed.").ApplyDefaults(HttpContext));
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound(new ProblemDetails { Title = "Role not found." }.ApplyDefaults(HttpContext));
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
                Detail = "Role is assigned to one or more users. Remove assignments before deleting.",
                Status = StatusCodes.Status422UnprocessableEntity
            }.ApplyDefaults(HttpContext));
        }

        var result = await _roleService.DeleteRoleAsync(role);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Role deletion failed.",
                Detail = string.Join("; ", result.Errors.Select(error => error.Description)),
                Status = StatusCodes.Status422UnprocessableEntity
            }.ApplyDefaults(HttpContext));
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
            return NotFound(new ProblemDetails { Title = "Role not found." }.ApplyDefaults(HttpContext));
        }

        var validation = new AdminValidationResult();
        var permissionIds = request.PermissionIds?.Distinct().ToList() ?? new List<Guid>();
        if (permissionIds.Count > 0)
        {
            var existingIds = await _dbContext.Permissions
                .Where(permission => permissionIds.Contains(permission.Id))
                .Select(permission => permission.Id)
                .ToListAsync(cancellationToken);
            var invalid = permissionIds.Except(existingIds).ToList();
            if (invalid.Count > 0)
            {
                validation.AddFieldError("permissionIds", $"Unknown permissions: {string.Join(", ", invalid)}.");
            }
        }

        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var systemAdminId = await _dbContext.Permissions
                .Where(permission => permission.Key == Company.Auth.Contracts.AuthConstants.Permissions.SystemAdmin)
                .Select(permission => permission.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (systemAdminId != Guid.Empty && !permissionIds.Contains(systemAdminId))
            {
                validation.AddFieldError("permissionIds", "Admin role must include the system.admin permission.");
            }
        }

        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid role permissions.").ApplyDefaults(HttpContext));
        }

        await _roleService.SetRolePermissionsAsync(id, permissionIds, cancellationToken);
        return NoContent();
    }
}
