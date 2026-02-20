using AuthServer.Data;
using AuthServer.Services.Admin.Models;

namespace AuthServer.Services.Admin;

/// <summary>
/// Defines the contract for admin permission admin service.
/// </summary>
public interface IAdminPermissionAdminService
{
    Task<IReadOnlyList<Permission>> GetPermissionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PermissionGroup>> GetGroupedPermissionsAsync(CancellationToken cancellationToken);
    Task<Permission?> GetPermissionAsync(Guid permissionId, CancellationToken cancellationToken);
    Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken cancellationToken);
    Task UpdatePermissionAsync(Permission permission, CancellationToken cancellationToken);
    Task DeletePermissionAsync(Guid permissionId, CancellationToken cancellationToken);
}
