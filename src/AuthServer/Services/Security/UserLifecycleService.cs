using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Governance;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Security;

public sealed class UserLifecycleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenRevocationService _tokenRevocationService;

    public UserLifecycleService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        TokenRevocationService tokenRevocationService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _tokenRevocationService = tokenRevocationService;
    }

    public async Task<UserLifecyclePolicy> GetPolicyAsync(CancellationToken cancellationToken)
    {
        var policy = await _dbContext.UserLifecyclePolicies.OrderBy(p => p.UpdatedUtc).FirstOrDefaultAsync(cancellationToken);
        if (policy is not null) return policy;

        policy = new UserLifecyclePolicy { Id = Guid.NewGuid(), UpdatedUtc = DateTime.UtcNow };
        _dbContext.UserLifecyclePolicies.Add(policy);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task<UserLifecyclePolicy> UpdatePolicyAsync(UserLifecyclePolicy updated, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var policy = await GetPolicyAsync(cancellationToken);
        policy.Enabled = updated.Enabled;
        policy.DeactivateAfterDays = updated.DeactivateAfterDays;
        policy.DeleteAfterDays = updated.DeleteAfterDays;
        policy.HardDeleteAfterDays = updated.HardDeleteAfterDays;
        policy.HardDeleteEnabled = updated.HardDeleteEnabled;
        policy.WarnBeforeLogoutMinutes = updated.WarnBeforeLogoutMinutes;
        policy.IdleLogoutMinutes = updated.IdleLogoutMinutes;
        policy.UpdatedUtc = DateTime.UtcNow;
        policy.UpdatedByUserId = actorUserId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task TrackLastSeenAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var activity = await _dbContext.UserActivities.FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
        if (activity is null)
        {
            _dbContext.UserActivities.Add(new UserActivity { UserId = userId, LastSeenUtc = utcNow, UpdatedUtc = utcNow });
        }
        else
        {
            activity.LastSeenUtc = utcNow;
            activity.UpdatedUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackLoginAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var activity = await _dbContext.UserActivities.FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
        if (activity is null)
        {
            _dbContext.UserActivities.Add(new UserActivity { UserId = userId, LastSeenUtc = utcNow, LastLoginUtc = utcNow, UpdatedUtc = utcNow });
        }
        else
        {
            activity.LastSeenUtc = utcNow;
            activity.LastLoginUtc = utcNow;
            activity.UpdatedUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task TrackPasswordChangeAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var activity = await _dbContext.UserActivities.FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
        if (activity is null)
        {
            _dbContext.UserActivities.Add(new UserActivity { UserId = userId, LastSeenUtc = utcNow, LastPasswordChangeUtc = utcNow, UpdatedUtc = utcNow });
        }
        else
        {
            activity.LastSeenUtc = utcNow;
            activity.LastPasswordChangeUtc = utcNow;
            activity.UpdatedUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(ApplicationUser user, Guid? actorUserId, CancellationToken cancellationToken)
    {
        if (user.IsAnonymized) return;
        if (user.IsActive)
        {
            user.IsActive = false;
            user.DeactivatedUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }

        await RevokeTokensAsync(user.Id, actorUserId, cancellationToken);
        await AddAuditAsync(actorUserId, "user.deactivated.inactivity", user.Id, null, cancellationToken);
    }

    public async Task AnonymizeAsync(ApplicationUser user, Guid? actorUserId, CancellationToken cancellationToken)
    {
        await RevokeTokensAsync(user.Id, actorUserId, cancellationToken);

        var deletedUsername = $"deleted_{user.Id:N}";
        var deletedEmail = $"deleted_{user.Id:N}@deleted.local";
        user.IsActive = false;
        user.IsAnonymized = true;
        user.AnonymizedUtc = DateTime.UtcNow;
        user.DeactivatedUtc ??= DateTime.UtcNow;
        user.Email = deletedEmail;
        user.NormalizedEmail = deletedEmail.ToUpperInvariant();
        user.UserName = deletedUsername;
        user.NormalizedUserName = deletedUsername.ToUpperInvariant();
        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;
        user.EmailConfirmed = false;
        user.TwoFactorEnabled = false;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.PasswordHash = null;

        _dbContext.UserClaims.RemoveRange(_dbContext.UserClaims.Where(claim => claim.UserId == user.Id));
        _dbContext.UserLogins.RemoveRange(_dbContext.UserLogins.Where(login => login.UserId == user.Id));
        _dbContext.UserTokens.RemoveRange(_dbContext.UserTokens.Where(token => token.UserId == user.Id));

        await _userManager.UpdateAsync(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "user.anonymized", user.Id, null, cancellationToken);
    }

    public async Task HardDeleteAsync(ApplicationUser user, Guid? actorUserId, CancellationToken cancellationToken)
    {
        if (!user.IsAnonymized) throw new InvalidOperationException("User must be anonymized before hard delete.");

        await RevokeTokensAsync(user.Id, actorUserId, cancellationToken);
        _dbContext.UserClaims.RemoveRange(_dbContext.UserClaims.Where(claim => claim.UserId == user.Id));
        _dbContext.UserLogins.RemoveRange(_dbContext.UserLogins.Where(login => login.UserId == user.Id));
        _dbContext.UserTokens.RemoveRange(_dbContext.UserTokens.Where(token => token.UserId == user.Id));
        _dbContext.UserRoles.RemoveRange(_dbContext.UserRoles.Where(role => role.UserId == user.Id));
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "user.hard-deleted", user.Id, null, cancellationToken);
    }

    public async Task<(int WouldDeactivate, int WouldAnonymize, int WouldHardDelete)> PreviewAsync(int days, CancellationToken cancellationToken)
    {
        var threshold = DateTime.UtcNow.AddDays(-days);
        var deact = await _dbContext.UserActivities.Where(a => a.LastSeenUtc <= threshold)
            .Join(_dbContext.Users.Where(u => u.IsActive && !u.IsAnonymized), a => a.UserId, u => u.Id, (_, _) => 1)
            .CountAsync(cancellationToken);
        var anon = await _dbContext.UserActivities.Where(a => a.LastSeenUtc <= threshold)
            .Join(_dbContext.Users.Where(u => !u.IsAnonymized), a => a.UserId, u => u.Id, (_, _) => 1)
            .CountAsync(cancellationToken);
        var hard = await _dbContext.UserActivities.Where(a => a.LastSeenUtc <= threshold)
            .Join(_dbContext.Users.Where(u => u.IsAnonymized), a => a.UserId, u => u.Id, (_, _) => 1)
            .CountAsync(cancellationToken);
        return (deact, anon, hard);
    }

    private async Task RevokeTokensAsync(Guid userId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var result = await _tokenRevocationService.RevokeUserAsync(userId, cancellationToken);
        await AddAuditAsync(actorUserId, "user.tokens.revoked", userId, new { result.TokensRevoked, result.AuthorizationsRevoked }, cancellationToken);
    }

    private async Task AddAuditAsync(Guid? actorUserId, string action, Guid targetUserId, object? data, CancellationToken cancellationToken)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = actorUserId,
            Action = action,
            TargetType = "User",
            TargetId = targetUserId.ToString(),
            DataJson = data is null ? null : JsonSerializer.Serialize(data)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
