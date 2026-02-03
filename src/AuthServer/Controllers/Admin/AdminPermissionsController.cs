using AuthServer.Data;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/permissions")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminPermissionsController : ControllerBase
{
    private readonly IAdminPermissionAdminService _permissionService;

    public AdminPermissionsController(IAdminPermissionAdminService permissionService)
    {
        _permissionService = permissionService;
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

    [HttpPost]
    public async Task<ActionResult<AdminPermissionItem>> CreatePermission(
        [FromBody] AdminPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var key = request.Key?.Trim() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;
        var group = request.Group?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(description))
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Invalid permission.",
                Detail = "Permission key and description are required."
            });
        }
        var existing = await _permissionService.GetPermissionsAsync(cancellationToken);
        if (existing.Any(permission => string.Equals(permission.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationProblem(new ValidationProblemDetails
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
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(description))
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Invalid permission.",
                Detail = "Permission key and description are required."
            });
        }
        var existing = await _permissionService.GetPermissionsAsync(cancellationToken);
        if (existing.Any(item => item.Id != id && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationProblem(new ValidationProblemDetails
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
        await _permissionService.DeletePermissionAsync(id, cancellationToken);
        return NoContent();
    }
}
