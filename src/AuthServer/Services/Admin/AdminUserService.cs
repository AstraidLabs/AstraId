using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using AuthServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthServer.Services.Admin;

/// <summary>
/// Provides admin user service functionality.
/// </summary>
public sealed class AdminUserService : IAdminUserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailSender _emailSender;
    private readonly UiUrlBuilder _uiUrlBuilder;

    public AdminUserService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        IEmailSender emailSender,
        UiUrlBuilder uiUrlBuilder)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _emailSender = emailSender;
        _uiUrlBuilder = uiUrlBuilder;
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
        var users = await query
            .OrderBy(user => user.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(user => user.Id).ToList();
        var roleLookup = await _dbContext.UserRoles
            .Where(userRole => userIds.Contains(userRole.UserId))
            .Join(_dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.UserId, RoleName = role.Name ?? string.Empty })
            .ToListAsync(cancellationToken);

        var rolesByUser = roleLookup
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(item => item.RoleName).OrderBy(name => name).ToList());

        var items = users
            .Select(user => new AdminUserListItem(
                user.Id,
                user.Email,
                user.UserName,
                user.EmailConfirmed,
                user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                user.IsActive,
                rolesByUser.TryGetValue(user.Id, out var roles) ? roles : Array.Empty<string>()))
            .ToList();

        return new PagedResult<AdminUserListItem>(items, totalCount, page, pageSize);
    }

    public async Task<ApplicationUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<ApplicationUser> CreateUserAsync(AdminUserCreateRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var userName = string.IsNullOrWhiteSpace(request.UserName) ? email : request.UserName.Trim();
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        var user = new ApplicationUser
        {
            Email = email,
            UserName = userName,
            PhoneNumber = phone,
            EmailConfirmed = false,
            IsActive = true
        };

        IdentityResult result;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            result = await _userManager.CreateAsync(user, request.Password);
        }
        else
        {
            result = await _userManager.CreateAsync(user);
        }

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        await LogAuditAsync("user.created", "User", user.Id.ToString(), new { user.Email, user.UserName });
        await SendActivationEmailAsync(user, cancellationToken);

        return user;
    }

    public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user, AdminUserUpdateRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<IdentityError>();

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _userManager.SetEmailAsync(user, request.Email.Trim());
            if (!emailResult.Succeeded)
            {
                errors.AddRange(emailResult.Errors);
            }
        }

        var nextUserName = string.IsNullOrWhiteSpace(request.UserName) ? request.Email.Trim() : request.UserName.Trim();
        if (!string.Equals(user.UserName, nextUserName, StringComparison.OrdinalIgnoreCase))
        {
            var userNameResult = await _userManager.SetUserNameAsync(user, nextUserName);
            if (!userNameResult.Succeeded)
            {
                errors.AddRange(userNameResult.Errors);
            }
        }

        var nextPhone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        if (!string.Equals(user.PhoneNumber, nextPhone, StringComparison.OrdinalIgnoreCase))
        {
            var phoneResult = await _userManager.SetPhoneNumberAsync(user, nextPhone);
            if (!phoneResult.Succeeded)
            {
                errors.AddRange(phoneResult.Errors);
            }
        }

        if (user.EmailConfirmed != request.EmailConfirmed)
        {
            user.EmailConfirmed = request.EmailConfirmed;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                errors.AddRange(updateResult.Errors);
            }
        }

        if (user.IsActive != request.IsActive)
        {
            user.IsActive = request.IsActive;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                errors.AddRange(updateResult.Errors);
            }
        }

        if (errors.Count > 0)
        {
            return IdentityResult.Failed(errors.ToArray());
        }

        await LogAuditAsync("user.updated", "User", user.Id.ToString(), new
        {
            user.Email,
            user.UserName,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.IsActive
        });

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeactivateUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await LogAuditAsync("user.deactivated", "User", user.Id.ToString(), new { user.Email });
        }

        return result;
    }

    public async Task<IdentityResult> ResendActivationAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
        {
            return IdentityResult.Failed(new IdentityError { Description = "Activation is not available for this user." });
        }

        await SendActivationEmailAsync(user, cancellationToken);
        await LogAuditAsync("user.activation.resent", "User", user.Id.ToString(), new { user.Email });

        return IdentityResult.Success;
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

    private async Task SendActivationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = EncodeToken(confirmationToken);
        var activationLink = _uiUrlBuilder.BuildActivationUrl(user.Email, encodedToken);
        var (subject, htmlBody, textBody) = EmailTemplates.BuildActivationEmail(activationLink);
        await _emailSender.SendAsync(
            new EmailMessage(user.Email, user.UserName, subject, htmlBody, textBody),
            cancellationToken);
    }

    private static string EncodeToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return WebEncoders.Base64UrlEncode(bytes);
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
