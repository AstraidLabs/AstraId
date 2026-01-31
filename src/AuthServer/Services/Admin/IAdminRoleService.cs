using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Services.Admin;

public interface IAdminRoleService
{
    Task<IReadOnlyList<AdminRoleListItem>> GetRolesAsync(CancellationToken cancellationToken);
    Task<IdentityRole<Guid>?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken);
    Task<IdentityResult> CreateRoleAsync(string roleName);
    Task<IdentityResult> DeleteRoleAsync(IdentityRole<Guid> role);
    Task<IReadOnlyList<Permission>> GetPermissionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetRolePermissionIdsAsync(Guid roleId, CancellationToken cancellationToken);
    Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken);
}
