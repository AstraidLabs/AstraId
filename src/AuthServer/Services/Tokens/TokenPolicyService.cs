using AuthServer.Data;
using AuthServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Tokens;

/// <summary>
/// Provides token policy service functionality.
/// </summary>
public sealed class TokenPolicyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptionsMonitor<TokenPolicyDefaultsOptions> _defaults;
    private readonly IOptionsMonitor<GovernanceGuardrailsOptions> _guardrails;
    private readonly ILogger<TokenPolicyService> _logger;

    public TokenPolicyService(
        ApplicationDbContext dbContext,
        IOptionsMonitor<TokenPolicyDefaultsOptions> defaults,
        IOptionsMonitor<GovernanceGuardrailsOptions> guardrails,
        ILogger<TokenPolicyService> logger)
    {
        _dbContext = dbContext;
        _defaults = defaults;
        _guardrails = guardrails;
        _logger = logger;
    }

    public async Task<TokenPolicySnapshot> GetEffectivePolicyAsync(CancellationToken cancellationToken)
    {
        var policy = await _dbContext.TokenPolicies.FirstOrDefaultAsync(cancellationToken);
        if (policy is null)
        {
            policy = await CreateDefaultAsync(cancellationToken);
        }

        return ToSnapshot(policy);
    }

    public async Task<TokenPolicySnapshot> UpdatePolicyAsync(
        TokenPolicySnapshot snapshot,
        Guid? updatedByUserId,
        CancellationToken cancellationToken)
    {
        var policy = await _dbContext.TokenPolicies.FirstOrDefaultAsync(cancellationToken)
            ?? await CreateDefaultAsync(cancellationToken);

        policy.AccessTokenMinutes = snapshot.AccessTokenMinutes;
        policy.IdentityTokenMinutes = snapshot.IdentityTokenMinutes;
        policy.AuthorizationCodeMinutes = snapshot.AuthorizationCodeMinutes;
        policy.RefreshTokenDays = snapshot.RefreshTokenDays;
        policy.RefreshRotationEnabled = snapshot.RefreshRotationEnabled;
        policy.RefreshReuseDetectionEnabled = snapshot.RefreshReuseDetectionEnabled;
        policy.RefreshReuseLeewaySeconds = snapshot.RefreshReuseLeewaySeconds;
        policy.ClockSkewSeconds = snapshot.ClockSkewSeconds;
        policy.UpdatedUtc = DateTime.UtcNow;
        policy.UpdatedByUserId = updatedByUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Token policy updated at {Timestamp}.", policy.UpdatedUtc);
        return ToSnapshot(policy);
    }

    public GovernanceGuardrailsOptions GetGuardrails() => _guardrails.CurrentValue;

    private async Task<TokenPolicy> CreateDefaultAsync(CancellationToken cancellationToken)
    {
        var defaults = _defaults.CurrentValue;
        var guardrails = _guardrails.CurrentValue;

        var policy = new TokenPolicy
        {
            Id = Guid.NewGuid(),
            AccessTokenMinutes = Clamp(defaults.AccessTokenMinutes, guardrails.MinAccessTokenMinutes, guardrails.MaxAccessTokenMinutes),
            IdentityTokenMinutes = Clamp(defaults.IdentityTokenMinutes, guardrails.MinIdentityTokenMinutes, guardrails.MaxIdentityTokenMinutes),
            AuthorizationCodeMinutes = Clamp(defaults.AuthorizationCodeMinutes, guardrails.MinAuthorizationCodeMinutes, guardrails.MaxAuthorizationCodeMinutes),
            RefreshTokenDays = Clamp(defaults.RefreshTokenDays, guardrails.MinRefreshTokenDays, guardrails.MaxRefreshTokenDays),
            RefreshRotationEnabled = defaults.RefreshRotationEnabled,
            RefreshReuseDetectionEnabled = defaults.RefreshReuseDetectionEnabled,
            RefreshReuseLeewaySeconds = Math.Max(0, defaults.RefreshReuseLeewaySeconds),
            ClockSkewSeconds = Clamp(defaults.ClockSkewSeconds, guardrails.MinClockSkewSeconds, guardrails.MaxClockSkewSeconds),
            UpdatedUtc = DateTime.UtcNow
        };

        _dbContext.TokenPolicies.Add(policy);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized token policy defaults.");
        return policy;
    }

    private static TokenPolicySnapshot ToSnapshot(TokenPolicy policy)
    {
        return new TokenPolicySnapshot(
            policy.AccessTokenMinutes,
            policy.IdentityTokenMinutes,
            policy.AuthorizationCodeMinutes,
            policy.RefreshTokenDays,
            policy.RefreshRotationEnabled,
            policy.RefreshReuseDetectionEnabled,
            policy.RefreshReuseLeewaySeconds,
            policy.ClockSkewSeconds);
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}
