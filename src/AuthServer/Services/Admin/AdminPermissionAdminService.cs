using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Admin;

public sealed class AdminPermissionAdminService : IAdminPermissionAdminService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminPermissionAdminService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<Permission>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Permissions
            .OrderBy(permission => permission.Group)
            .ThenBy(permission => permission.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionGroup>> GetGroupedPermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = await GetPermissionsAsync(cancellationToken);
        return permissions
            .GroupBy(permission => string.IsNullOrWhiteSpace(permission.Group) ? "General" : permission.Group)
            .OrderBy(group => group.Key)
            .Select(group => new PermissionGroup(group.Key, group.ToList()))
            .ToList();
    }

    public async Task<Permission?> GetPermissionAsync(Guid permissionId, CancellationToken cancellationToken)
    {
        return await _dbContext.Permissions.FirstOrDefaultAsync(permission => permission.Id == permissionId, cancellationToken);
    }

    public async Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken cancellationToken)
    {
        permission.Id = Guid.NewGuid();
        _dbContext.Permissions.Add(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("permission.created", "Permission", permission.Id.ToString(), permission);
        return permission;
    }

    public async Task UpdatePermissionAsync(Permission permission, CancellationToken cancellationToken)
    {
        _dbContext.Permissions.Update(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("permission.updated", "Permission", permission.Id.ToString(), permission);
    }

    public async Task DeletePermissionAsync(Guid permissionId, CancellationToken cancellationToken)
    {
        var permission = await _dbContext.Permissions.FirstOrDefaultAsync(item => item.Id == permissionId, cancellationToken);
        if (permission is null)
        {
            return;
        }

        _dbContext.Permissions.Remove(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("permission.deleted", "Permission", permission.Id.ToString(), permission);
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
