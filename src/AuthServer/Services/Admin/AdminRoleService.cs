using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Admin;

/// <summary>
/// Provides admin role service functionality.
/// </summary>
public sealed class AdminRoleService : IAdminRoleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminRoleService(
        ApplicationDbContext dbContext,
        RoleManager<IdentityRole<Guid>> roleManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<AdminRoleListItem>> GetRolesAsync(CancellationToken cancellationToken)
    {
        return await _roleManager.Roles
            .OrderBy(role => role.Name)
            .Select(role => new AdminRoleListItem(role.Id, role.Name ?? string.Empty, role.Name == "Admin"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IdentityRole<Guid>?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return await _roleManager.Roles.FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);
    }

    public async Task<IdentityResult> CreateRoleAsync(string roleName)
    {
        var role = new IdentityRole<Guid> { Name = roleName };
        var result = await _roleManager.CreateAsync(role);
        if (result.Succeeded)
        {
            await LogAuditAsync("role.created", "Role", role.Id.ToString(), new { roleName });
        }

        return result;
    }

    public async Task<IdentityResult> UpdateRoleAsync(IdentityRole<Guid> role, string roleName)
    {
        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return IdentityResult.Failed(new IdentityError { Description = "System role cannot be renamed." });
        }

        role.Name = roleName;
        var result = await _roleManager.UpdateAsync(role);
        if (result.Succeeded)
        {
            await LogAuditAsync("role.updated", "Role", role.Id.ToString(), new { roleName });
        }

        return result;
    }

    public async Task<IdentityResult> DeleteRoleAsync(IdentityRole<Guid> role)
    {
        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return IdentityResult.Failed(new IdentityError { Description = "System role cannot be deleted." });
        }

        var result = await _roleManager.DeleteAsync(role);
        if (result.Succeeded)
        {
            await LogAuditAsync("role.deleted", "Role", role.Id.ToString(), new { role.Name });
        }

        return result;
    }

    public async Task<IReadOnlyList<Permission>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Permissions
            .OrderBy(permission => permission.Group)
            .ThenBy(permission => permission.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetRolePermissionIdsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return await _dbContext.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .Select(rolePermission => rolePermission.PermissionId)
            .ToListAsync(cancellationToken);
    }

    public async Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .ToListAsync(cancellationToken);

        var desired = permissionIds.Distinct().ToHashSet();
        var toRemove = existing.Where(rolePermission => !desired.Contains(rolePermission.PermissionId)).ToList();
        var existingIds = existing.Select(rolePermission => rolePermission.PermissionId).ToHashSet();

        foreach (var remove in toRemove)
        {
            _dbContext.RolePermissions.Remove(remove);
        }

        foreach (var permissionId in desired.Where(id => !existingIds.Contains(id)))
        {
            _dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("role.permissions.updated", "Role", roleId.ToString(), new { permissionIds = desired.ToArray() });
    }

    private async Task LogAuditAsync(string action, string targetType, string targetId, object data)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = GetActorUserId(),
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = JsonSerializer.Serialize(data)
        });

        await _dbContext.SaveChangesAsync();
    }

    private Guid? GetActorUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }
}
