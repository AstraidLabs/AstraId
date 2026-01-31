using AuthServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services;

public sealed class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public PermissionService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<string[]> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        var roleNames = await _userManager.GetRolesAsync(user);
        if (roleNames.Count == 0)
        {
            return [];
        }

        var roleIds = await _roleManager.Roles
            .Where(role => roleNames.Contains(role.Name!))
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        return await GetPermissionsForRolesAsync(roleIds, cancellationToken);
    }

    public async Task<string[]> GetPermissionsForRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.RolePermissions
            .Where(rolePermission => roleIds.Contains(rolePermission.RoleId))
            .Select(rolePermission => rolePermission.Permission!.Key)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }
}
