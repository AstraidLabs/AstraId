using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Admin;

public sealed class AdminUserService : IAdminUserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminUserService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResult<AdminUserListItem>> GetUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(user => (user.Email ?? string.Empty).Contains(search) || (user.UserName ?? string.Empty).Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(user => user.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new AdminUserListItem(
                user.Id,
                user.Email,
                user.UserName,
                user.EmailConfirmed,
                user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserListItem>(items, totalCount, page, pageSize);
    }

    public async Task<ApplicationUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<IdentityResult> SetUserRolesAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var currentRoles = await _userManager.GetRolesAsync(user);
        var roleList = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(roleList, StringComparer.OrdinalIgnoreCase));
        if (!removeResult.Succeeded)
        {
            return removeResult;
        }

        var addResult = await _userManager.AddToRolesAsync(user, roleList.Except(currentRoles, StringComparer.OrdinalIgnoreCase));
        if (addResult.Succeeded)
        {
            await LogAuditAsync("user.roles.updated", "User", user.Id.ToString(), new { roles = roleList });
        }

        return addResult;
    }

    public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string newPassword)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (result.Succeeded)
        {
            await LogAuditAsync("user.password.reset", "User", user.Id.ToString(), new { });
        }

        return result;
    }

    public async Task SetLockoutAsync(ApplicationUser user, bool isLockedOut, CancellationToken cancellationToken)
    {
        user.LockoutEnd = isLockedOut ? DateTimeOffset.UtcNow.AddYears(100) : null;
        await _userManager.UpdateAsync(user);

        await LogAuditAsync(isLockedOut ? "user.locked" : "user.unlocked", "User", user.Id.ToString(), new { isLockedOut });
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
