using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Governance;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Security;

public sealed class PrivacyGovernanceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserLifecycleService _lifecycleService;
    private readonly TokenRevocationService _tokenRevocationService;

    public PrivacyGovernanceService(ApplicationDbContext dbContext, UserLifecycleService lifecycleService, TokenRevocationService tokenRevocationService)
    {
        _dbContext = dbContext;
        _lifecycleService = lifecycleService;
        _tokenRevocationService = tokenRevocationService;
    }

    public async Task<PrivacyPolicy> GetPolicyAsync(CancellationToken cancellationToken)
    {
        var policy = await _dbContext.PrivacyPolicies.OrderBy(p => p.UpdatedUtc).FirstOrDefaultAsync(cancellationToken);
        if (policy is not null) return policy;

        policy = new PrivacyPolicy { Id = Guid.NewGuid(), UpdatedUtc = DateTime.UtcNow };
        _dbContext.PrivacyPolicies.Add(policy);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task<PrivacyPolicy> UpdatePolicyAsync(PrivacyPolicy updated, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var policy = await GetPolicyAsync(cancellationToken);
        policy.LoginHistoryRetentionDays = updated.LoginHistoryRetentionDays;
        policy.ErrorLogRetentionDays = updated.ErrorLogRetentionDays;
        policy.TokenRetentionDays = updated.TokenRetentionDays;
        policy.AuditLogRetentionDays = updated.AuditLogRetentionDays;
        policy.DeletionCooldownDays = updated.DeletionCooldownDays;
        policy.AnonymizeInsteadOfHardDelete = updated.AnonymizeInsteadOfHardDelete;
        policy.RequireMfaForDeletionRequest = updated.RequireMfaForDeletionRequest;
        policy.RequireRecentReauthForExport = updated.RequireRecentReauthForExport;
        policy.UpdatedByUserId = actorUserId;
        policy.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "privacy.policy.updated", "PrivacyPolicy", "global", policy, cancellationToken);
        return policy;
    }

    public async Task<DeletionRequest> CreateDeletionRequestAsync(Guid userId, string? reason, CancellationToken cancellationToken)
    {
        var active = await _dbContext.DeletionRequests.FirstOrDefaultAsync(r => r.UserId == userId && (r.Status == DeletionRequestStatus.Pending || r.Status == DeletionRequestStatus.Approved), cancellationToken);
        if (active is not null) return active;

        var policy = await GetPolicyAsync(cancellationToken);
        var request = new DeletionRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RequestedUtc = DateTime.UtcNow,
            Status = DeletionRequestStatus.Pending,
            Reason = reason,
            CooldownUntilUtc = DateTime.UtcNow.AddDays(policy.DeletionCooldownDays)
        };
        _dbContext.DeletionRequests.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(userId, "privacy.deletion.requested", "DeletionRequest", request.Id.ToString(), new { request.CooldownUntilUtc }, cancellationToken);
        return request;
    }

    public async Task<bool> CancelDeletionRequestAsync(Guid userId, CancellationToken cancellationToken)
    {
        var request = await _dbContext.DeletionRequests
            .Where(r => r.UserId == userId && (r.Status == DeletionRequestStatus.Pending || r.Status == DeletionRequestStatus.Approved))
            .OrderByDescending(r => r.RequestedUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null || request.CooldownUntilUtc < DateTime.UtcNow) return false;

        request.Status = DeletionRequestStatus.Cancelled;
        request.CancelUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(userId, "privacy.deletion.cancelled", "DeletionRequest", request.Id.ToString(), null, cancellationToken);
        return true;
    }

    public async Task ExecuteErasureAsync(DeletionRequest request, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null || request.Status is DeletionRequestStatus.Executed or DeletionRequestStatus.Cancelled) return;

        var policy = await GetPolicyAsync(cancellationToken);
        await _tokenRevocationService.RevokeUserAsync(user.Id, cancellationToken);

        if (policy.AnonymizeInsteadOfHardDelete)
        {
            await _lifecycleService.AnonymizeAsync(user, actorUserId, cancellationToken);
        }
        else
        {
            await _lifecycleService.AnonymizeAsync(user, actorUserId, cancellationToken);
            await _lifecycleService.HardDeleteAsync(user, actorUserId, cancellationToken);
        }

        request.Status = DeletionRequestStatus.Executed;
        request.ExecutedUtc = DateTime.UtcNow;
        request.ApprovedBy ??= actorUserId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "privacy.deletion.executed", "DeletionRequest", request.Id.ToString(), null, cancellationToken);
    }

    public async Task AddAuditAsync(Guid? actorUserId, string action, string targetType, string targetId, object? data, CancellationToken cancellationToken)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = data is null ? null : JsonSerializer.Serialize(data)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
