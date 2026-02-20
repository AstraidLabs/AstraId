using System.Text.Json;
using AuthServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Security;

/// <summary>
/// Provides inactivity policy service functionality.
/// </summary>
public sealed class InactivityPolicyService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public InactivityPolicyService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<InactivityPolicy> GetAsync(CancellationToken cancellationToken)
    {
        var policy = await _db.InactivityPolicies.OrderBy(x => x.UpdatedUtc).FirstOrDefaultAsync(cancellationToken);
        if (policy is not null)
        {
            return policy;
        }

        policy = new InactivityPolicy { Id = Guid.NewGuid(), UpdatedUtc = DateTime.UtcNow };
        _db.InactivityPolicies.Add(policy);
        await _db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task<InactivityPolicy> UpdateAsync(InactivityPolicy request, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var policy = await GetAsync(cancellationToken);
        policy.Enabled = request.Enabled;
        policy.WarningAfterDays = request.WarningAfterDays;
        policy.DeactivateAfterDays = request.DeactivateAfterDays;
        policy.DeleteAfterDays = request.DeleteAfterDays;
        policy.WarningRepeatDays = request.WarningRepeatDays;
        policy.DeleteMode = request.DeleteMode;
        policy.ProtectAdmins = request.ProtectAdmins;
        policy.ProtectedRoles = request.ProtectedRoles;
        policy.UpdatedUtc = DateTime.UtcNow;
        policy.UpdatedByUserId = actorUserId;
        await _db.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "inactivity.policy.updated", "InactivityPolicy", policy.Id.ToString(), policy, cancellationToken);
        return policy;
    }

    public async Task<bool> IsProtectedAsync(ApplicationUser user, InactivityPolicy policy)
    {
        if (!policy.ProtectAdmins)
        {
            return false;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var protectedRoles = (policy.ProtectedRoles ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return roles.Any(protectedRoles.Contains);
    }

    private async Task AddAuditAsync(Guid? actorUserId, string action, string targetType, string? targetId, object? data, CancellationToken cancellationToken)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = data is null ? null : JsonSerializer.Serialize(data)
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
