using AuthServer.Data;
using AuthServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Governance;

public sealed class GovernancePolicyStore
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptionsMonitor<KeyRotationDefaultsOptions> _keyDefaults;
    private readonly IOptionsMonitor<TokenPolicyDefaultsOptions> _tokenDefaults;
    private readonly IOptionsMonitor<GovernanceGuardrailsOptions> _guardrails;
    private readonly ILogger<GovernancePolicyStore> _logger;

    public GovernancePolicyStore(
        ApplicationDbContext dbContext,
        IOptionsMonitor<KeyRotationDefaultsOptions> keyDefaults,
        IOptionsMonitor<TokenPolicyDefaultsOptions> tokenDefaults,
        IOptionsMonitor<GovernanceGuardrailsOptions> guardrails,
        ILogger<GovernancePolicyStore> logger)
    {
        _dbContext = dbContext;
        _keyDefaults = keyDefaults;
        _tokenDefaults = tokenDefaults;
        _guardrails = guardrails;
        _logger = logger;
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken)
    {
        await EnsureKeyRotationPolicyAsync(cancellationToken);
        await EnsureTokenPolicyAsync(cancellationToken);
    }

    private async Task EnsureKeyRotationPolicyAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.KeyRotationPolicies.AnyAsync(cancellationToken))
        {
            return;
        }

        var defaults = _keyDefaults.CurrentValue;
        var guardrails = _guardrails.CurrentValue;
        var now = DateTime.UtcNow;
        var rotationInterval = Clamp(defaults.RotationIntervalDays, guardrails.MinRotationIntervalDays, guardrails.MaxRotationIntervalDays);
        var grace = Clamp(defaults.GracePeriodDays, guardrails.MinGracePeriodDays, guardrails.MaxGracePeriodDays);

        _dbContext.KeyRotationPolicies.Add(new KeyRotationPolicy
        {
            Id = Guid.NewGuid(),
            Enabled = defaults.Enabled,
            RotationIntervalDays = rotationInterval,
            GracePeriodDays = grace,
            NextRotationUtc = now.AddDays(rotationInterval),
            UpdatedUtc = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized key rotation policy defaults.");
    }

    private async Task EnsureTokenPolicyAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.TokenPolicies.AnyAsync(cancellationToken))
        {
            return;
        }

        var defaults = _tokenDefaults.CurrentValue;
        var guardrails = _guardrails.CurrentValue;

        _dbContext.TokenPolicies.Add(new TokenPolicy
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
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized token policy defaults.");
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}
