using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Services.Admin;

/// <summary>
/// Defines the contract for admin user service.
/// </summary>
public interface IAdminUserService
{
    Task<PagedResult<AdminUserListItem>> GetUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<ApplicationUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<ApplicationUser> CreateUserAsync(AdminUserCreateRequest request, CancellationToken cancellationToken);
    Task<IdentityResult> UpdateUserAsync(ApplicationUser user, AdminUserUpdateRequest request, CancellationToken cancellationToken);
    Task<IdentityResult> DeactivateUserAsync(ApplicationUser user, CancellationToken cancellationToken);
    Task<IdentityResult> ResendActivationAsync(ApplicationUser user, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetUserRolesAsync(ApplicationUser user);
    Task<IdentityResult> SetUserRolesAsync(ApplicationUser user, IEnumerable<string> roles);
    Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string newPassword);
    Task SetLockoutAsync(ApplicationUser user, bool isLockedOut, CancellationToken cancellationToken);
}
