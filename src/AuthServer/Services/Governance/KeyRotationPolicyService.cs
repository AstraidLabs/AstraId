using AuthServer.Data;
using AuthServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Governance;

/// <summary>
/// Provides key rotation policy service functionality.
/// </summary>
public sealed class KeyRotationPolicyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptionsMonitor<GovernanceGuardrailsOptions> _guardrails;
    private readonly ILogger<KeyRotationPolicyService> _logger;

    public KeyRotationPolicyService(
        ApplicationDbContext dbContext,
        IOptionsMonitor<GovernanceGuardrailsOptions> guardrails,
        ILogger<KeyRotationPolicyService> logger)
    {
        _dbContext = dbContext;
        _guardrails = guardrails;
        _logger = logger;
    }

    public async Task<KeyRotationPolicy> GetPolicyAsync(CancellationToken cancellationToken)
    {
        var policy = await _dbContext.KeyRotationPolicies.FirstOrDefaultAsync(cancellationToken);
        if (policy is null)
        {
            throw new InvalidOperationException("Key rotation policy has not been initialized.");
        }

        return policy;
    }

    public GovernanceGuardrailsOptions GetGuardrails() => _guardrails.CurrentValue;

    public async Task<KeyRotationPolicy> UpdatePolicyAsync(
        bool enabled,
        int rotationIntervalDays,
        int gracePeriodDays,
        int jwksCacheMarginMinutes,
        Guid? updatedByUserId,
        CancellationToken cancellationToken)
    {
        var policy = await GetPolicyAsync(cancellationToken);
        var now = DateTime.UtcNow;

        policy.Enabled = enabled;
        policy.RotationIntervalDays = rotationIntervalDays;
        policy.GracePeriodDays = gracePeriodDays;
        policy.JwksCacheMarginMinutes = jwksCacheMarginMinutes;
        policy.UpdatedUtc = now;
        policy.UpdatedByUserId = updatedByUserId;

        if (policy.LastRotationUtc.HasValue)
        {
            policy.NextRotationUtc = policy.LastRotationUtc.Value.AddDays(rotationIntervalDays);
        }
        else if (policy.NextRotationUtc is null)
        {
            policy.NextRotationUtc = now.AddDays(rotationIntervalDays);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Key rotation policy updated at {Timestamp}.", now);
        return policy;
    }
}
