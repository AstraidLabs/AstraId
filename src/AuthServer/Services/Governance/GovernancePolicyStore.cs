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
    private readonly IOptionsMonitor<OAuthAdvancedPolicyDefaultsOptions> _oauthAdvancedDefaults;
    private readonly ILogger<GovernancePolicyStore> _logger;

    public GovernancePolicyStore(
        ApplicationDbContext dbContext,
        IOptionsMonitor<KeyRotationDefaultsOptions> keyDefaults,
        IOptionsMonitor<TokenPolicyDefaultsOptions> tokenDefaults,
        IOptionsMonitor<GovernanceGuardrailsOptions> guardrails,
        IOptionsMonitor<OAuthAdvancedPolicyDefaultsOptions> oauthAdvancedDefaults,
        ILogger<GovernancePolicyStore> logger)
    {
        _dbContext = dbContext;
        _keyDefaults = keyDefaults;
        _tokenDefaults = tokenDefaults;
        _guardrails = guardrails;
        _oauthAdvancedDefaults = oauthAdvancedDefaults;
        _logger = logger;
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken)
    {
        await EnsureKeyRotationPolicyAsync(cancellationToken);
        await EnsureTokenPolicyAsync(cancellationToken);
        await EnsureOAuthAdvancedPolicyAsync(cancellationToken);
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
        var jwksMargin = Clamp(defaults.JwksCacheMarginMinutes, guardrails.MinJwksCacheMarginMinutes, guardrails.MaxJwksCacheMarginMinutes);

        _dbContext.KeyRotationPolicies.Add(new KeyRotationPolicy
        {
            Id = Guid.NewGuid(),
            Enabled = defaults.Enabled,
            RotationIntervalDays = rotationInterval,
            GracePeriodDays = grace,
            JwksCacheMarginMinutes = jwksMargin,
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


    private async Task EnsureOAuthAdvancedPolicyAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.OAuthAdvancedPolicies.AnyAsync(cancellationToken))
        {
            return;
        }

        var defaults = _oauthAdvancedDefaults.CurrentValue;
        _dbContext.OAuthAdvancedPolicies.Add(new OAuthAdvancedPolicy
        {
            Id = Guid.NewGuid(),
            DeviceFlowEnabled = defaults.DeviceFlowEnabled,
            DeviceFlowUserCodeTtlMinutes = Clamp(defaults.DeviceFlowUserCodeTtlMinutes, 1, 60),
            DeviceFlowPollingIntervalSeconds = Math.Max(5, defaults.DeviceFlowPollingIntervalSeconds),
            TokenExchangeEnabled = defaults.TokenExchangeEnabled,
            TokenExchangeAllowedClientIdsJson = System.Text.Json.JsonSerializer.Serialize(defaults.TokenExchangeAllowedClientIds),
            TokenExchangeAllowedAudiencesJson = System.Text.Json.JsonSerializer.Serialize(defaults.TokenExchangeAllowedAudiences),
            RefreshRotationEnabled = defaults.RefreshRotationEnabled,
            RefreshReuseDetectionEnabled = defaults.RefreshReuseDetectionEnabled,
            RefreshReuseAction = defaults.RefreshReuseAction,
            BackChannelLogoutEnabled = defaults.BackChannelLogoutEnabled,
            FrontChannelLogoutEnabled = defaults.FrontChannelLogoutEnabled,
            LogoutTokenTtlMinutes = Clamp(defaults.LogoutTokenTtlMinutes, 1, 60),
            UpdatedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized OAuth advanced policy defaults.");
    }
    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}
