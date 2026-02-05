using AuthServer.Data;
using AuthServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Tokens;

public sealed class TokenPolicyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptionsMonitor<AuthServerTokenOptions> _options;
    private readonly ILogger<TokenPolicyService> _logger;

    public TokenPolicyService(
        ApplicationDbContext dbContext,
        IOptionsMonitor<AuthServerTokenOptions> options,
        ILogger<TokenPolicyService> logger)
    {
        _dbContext = dbContext;
        _options = options;
        _logger = logger;
    }

    public async Task<TokenPolicySnapshot> GetEffectivePolicyAsync(CancellationToken cancellationToken)
    {
        var overrides = await _dbContext.TokenPolicyOverrides.FirstOrDefaultAsync(cancellationToken);
        return BuildSnapshot(_options.CurrentValue, overrides);
    }

    public async Task<TokenPolicySnapshot> UpdateOverridesAsync(TokenPolicySnapshot snapshot, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.TokenPolicyOverrides.FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            entity = new TokenPolicyOverride { Id = Guid.NewGuid() };
            _dbContext.TokenPolicyOverrides.Add(entity);
        }

        entity.PublicAccessTokenMinutes = snapshot.Public.AccessTokenMinutes;
        entity.PublicIdentityTokenMinutes = snapshot.Public.IdentityTokenMinutes;
        entity.PublicRefreshTokenAbsoluteDays = snapshot.Public.RefreshTokenAbsoluteDays;
        entity.PublicRefreshTokenSlidingDays = snapshot.Public.RefreshTokenSlidingDays;
        entity.ConfidentialAccessTokenMinutes = snapshot.Confidential.AccessTokenMinutes;
        entity.ConfidentialIdentityTokenMinutes = snapshot.Confidential.IdentityTokenMinutes;
        entity.ConfidentialRefreshTokenAbsoluteDays = snapshot.Confidential.RefreshTokenAbsoluteDays;
        entity.ConfidentialRefreshTokenSlidingDays = snapshot.Confidential.RefreshTokenSlidingDays;
        entity.RefreshRotationEnabled = snapshot.RefreshPolicy.RotationEnabled;
        entity.RefreshReuseDetectionEnabled = snapshot.RefreshPolicy.ReuseDetectionEnabled;
        entity.RefreshReuseLeewaySeconds = snapshot.RefreshPolicy.ReuseLeewaySeconds;
        entity.UpdatedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Token policy overrides updated at {Timestamp}.", entity.UpdatedUtc);
        return BuildSnapshot(_options.CurrentValue, entity);
    }

    public TokenPolicySnapshot BuildSnapshot(AuthServerTokenOptions options, TokenPolicyOverride? overrides)
    {
        var publicPreset = new TokenPreset(
            overrides?.PublicAccessTokenMinutes ?? options.Public.AccessTokenMinutes,
            overrides?.PublicIdentityTokenMinutes ?? options.Public.IdentityTokenMinutes,
            overrides?.PublicRefreshTokenAbsoluteDays ?? options.Public.RefreshTokenAbsoluteDays,
            overrides?.PublicRefreshTokenSlidingDays ?? options.Public.RefreshTokenSlidingDays);

        var confidentialPreset = new TokenPreset(
            overrides?.ConfidentialAccessTokenMinutes ?? options.Confidential.AccessTokenMinutes,
            overrides?.ConfidentialIdentityTokenMinutes ?? options.Confidential.IdentityTokenMinutes,
            overrides?.ConfidentialRefreshTokenAbsoluteDays ?? options.Confidential.RefreshTokenAbsoluteDays,
            overrides?.ConfidentialRefreshTokenSlidingDays ?? options.Confidential.RefreshTokenSlidingDays);

        var refreshPolicy = new RefreshTokenPolicy(
            overrides?.RefreshRotationEnabled ?? options.RefreshPolicy.RotationEnabled,
            overrides?.RefreshReuseDetectionEnabled ?? options.RefreshPolicy.ReuseDetectionEnabled,
            overrides?.RefreshReuseLeewaySeconds ?? options.RefreshPolicy.ReuseLeewaySeconds);

        return new TokenPolicySnapshot(publicPreset, confidentialPreset, refreshPolicy);
    }
}
