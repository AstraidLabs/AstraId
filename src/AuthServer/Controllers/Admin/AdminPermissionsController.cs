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
/// Exposes HTTP endpoints for admin permissions.
/// </summary>

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
        var validation = AdminValidation.ValidatePermission(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid permission.").ApplyDefaults(HttpContext));
        }

        var key = request.Key!.Trim();
        var description = request.Description!.Trim();
        var group = request.Group?.Trim() ?? string.Empty;

        if (await _dbContext.Permissions.AnyAsync(
                permission => permission.Key.ToLower() == key.ToLower(),
                cancellationToken))
        {
            validation.AddFieldError("key", $"Permission key '{key}' is already in use.");
            return ValidationProblem(validation.ToProblemDetails("Permission already exists.").ApplyDefaults(HttpContext));
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

        var validation = AdminValidation.ValidatePermission(request);
        if (permission.IsSystem && !string.Equals(permission.Key, request.Key?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            validation.AddFieldError("key", "System permission keys cannot be changed.");
        }
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid permission.").ApplyDefaults(HttpContext));
        }

        var key = request.Key!.Trim();
        var description = request.Description!.Trim();
        var group = request.Group?.Trim() ?? string.Empty;

        if (await _dbContext.Permissions.AnyAsync(
                item => item.Id != id && item.Key.ToLower() == key.ToLower(),
                cancellationToken))
        {
            validation.AddFieldError("key", $"Permission key '{key}' is already in use.");
            return ValidationProblem(validation.ToProblemDetails("Permission already exists.").ApplyDefaults(HttpContext));
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

        if (permission.IsSystem)
        {
            var validation = new AdminValidationResult();
            validation.AddGeneralError("System permissions cannot be deleted.");
            return ValidationProblem(validation.ToProblemDetails("Permission is protected.").ApplyDefaults(HttpContext));
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
                Detail = "Permission is assigned to roles or endpoints. Remove assignments before deleting.",
                Status = StatusCodes.Status422UnprocessableEntity
            }.ApplyDefaults(HttpContext));
        }

        await _permissionService.DeletePermissionAsync(id, cancellationToken);
        return NoContent();
    }
}
