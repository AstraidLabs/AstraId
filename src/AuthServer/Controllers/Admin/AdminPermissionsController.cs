using AuthServer.Data;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/permissions")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminPermissionsController : ControllerBase
{
    private readonly IAdminPermissionAdminService _permissionService;
    private readonly ApplicationDbContext _dbContext;

    public AdminPermissionsController(IAdminPermissionAdminService permissionService, ApplicationDbContext dbContext)
    {
        _permissionService = permissionService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminPermissionItem>>> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _permissionService.GetPermissionsAsync(cancellationToken);
        var items = permissions
            .Select(permission => new AdminPermissionItem(
                permission.Id,
                permission.Key,
                permission.Description,
                permission.Group,
                permission.IsSystem))
            .ToList();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminPermissionItem>> GetPermission(Guid id, CancellationToken cancellationToken)
    {
        var permission = await _permissionService.GetPermissionAsync(id, cancellationToken);
        if (permission is null)
        {
            return NotFound();
        }

        return Ok(new AdminPermissionItem(
            permission.Id,
            permission.Key,
            permission.Description,
            permission.Group,
            permission.IsSystem));
    }

    [HttpGet("{id:guid}/usage")]
    public async Task<ActionResult<AdminPermissionUsage>> GetPermissionUsage(Guid id, CancellationToken cancellationToken)
    {
        var permission = await _permissionService.GetPermissionAsync(id, cancellationToken);
        if (permission is null)
        {
            return NotFound();
        }

        var roleCount = await _dbContext.RolePermissions.CountAsync(
            item => item.PermissionId == id,
            cancellationToken);
        var endpointCount = await _dbContext.EndpointPermissions.CountAsync(
            item => item.PermissionId == id,
            cancellationToken);

        return Ok(new AdminPermissionUsage(roleCount, endpointCount));
    }

    [HttpPost]
    public async Task<ActionResult<AdminPermissionItem>> CreatePermission(
        [FromBody] AdminPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var key = request.Key?.Trim() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;
        var group = request.Group?.Trim() ?? string.Empty;
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(key))
        {
            errors["key"] = ["Permission key is required."];
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            errors["description"] = ["Permission description is required."];
        }
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors)
            {
                Title = "Invalid permission.",
                Detail = "Permission key and description are required."
            });
        }
        var existing = await _permissionService.GetPermissionsAsync(cancellationToken);
        if (existing.Any(permission => string.Equals(permission.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["key"] = [$"Permission key '{key}' is already in use."]
            })
            {
                Title = "Permission already exists.",
                Detail = $"Permission key '{key}' is already in use."
            });
        }

        var permission = new Permission
        {
            Key = key,
            Description = description,
            Group = group,
            IsSystem = request.IsSystem
        };

        var created = await _permissionService.CreatePermissionAsync(permission, cancellationToken);
        var response = new AdminPermissionItem(
            created.Id,
            created.Key,
            created.Description,
            created.Group,
            created.IsSystem);
        return CreatedAtAction(nameof(GetPermission), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminPermissionItem>> UpdatePermission(
        Guid id,
        [FromBody] AdminPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var permission = await _permissionService.GetPermissionAsync(id, cancellationToken);
        if (permission is null)
        {
            return NotFound();
        }

        var key = request.Key?.Trim() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;
        var group = request.Group?.Trim() ?? string.Empty;
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(key))
        {
            errors["key"] = ["Permission key is required."];
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            errors["description"] = ["Permission description is required."];
        }
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors)
            {
                Title = "Invalid permission.",
                Detail = "Permission key and description are required."
            });
        }
        var existing = await _permissionService.GetPermissionsAsync(cancellationToken);
        if (existing.Any(item => item.Id != id && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["key"] = [$"Permission key '{key}' is already in use."]
            })
            {
                Title = "Permission already exists.",
                Detail = $"Permission key '{key}' is already in use."
            });
        }

        permission.Key = key;
        permission.Description = description;
        permission.Group = group;
        permission.IsSystem = request.IsSystem;

        await _permissionService.UpdatePermissionAsync(permission, cancellationToken);
        return Ok(new AdminPermissionItem(
            permission.Id,
            permission.Key,
            permission.Description,
            permission.Group,
            permission.IsSystem));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePermission(Guid id, CancellationToken cancellationToken)
    {
        var permission = await _permissionService.GetPermissionAsync(id, cancellationToken);
        if (permission is null)
        {
            return NotFound();
        }

        var roleCount = await _dbContext.RolePermissions.CountAsync(
            item => item.PermissionId == id,
            cancellationToken);
        var endpointCount = await _dbContext.EndpointPermissions.CountAsync(
            item => item.PermissionId == id,
            cancellationToken);

        if (roleCount > 0 || endpointCount > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["permission"] =
                [
                    "Permission is assigned to roles or endpoints. Remove assignments before deleting."
                ]
            })
            {
                Title = "Permission is in use.",
                Detail = "Permission is assigned to roles or endpoints. Remove assignments before deleting."
            });
        }

        await _permissionService.DeletePermissionAsync(id, cancellationToken);
        return NoContent();
    }
}
