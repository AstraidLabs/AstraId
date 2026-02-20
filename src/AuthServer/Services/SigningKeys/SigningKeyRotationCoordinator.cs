using AuthServer.Data;
using AuthServer.Services.Governance;
using AuthServer.Services.Tokens;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.SigningKeys;

/// <summary>
/// Provides signing key rotation coordinator functionality.
/// </summary>
public sealed class SigningKeyRotationCoordinator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SigningKeyRingService _keyRingService;
    private readonly TokenIncidentService _incidentService;
    private readonly TokenPolicyService _tokenPolicyService;
    private readonly ILogger<SigningKeyRotationCoordinator> _logger;

    public SigningKeyRotationCoordinator(
        ApplicationDbContext dbContext,
        SigningKeyRingService keyRingService,
        TokenIncidentService incidentService,
        TokenPolicyService tokenPolicyService,
        ILogger<SigningKeyRotationCoordinator> logger)
    {
        _dbContext = dbContext;
        _keyRingService = keyRingService;
        _incidentService = incidentService;
        _tokenPolicyService = tokenPolicyService;
        _logger = logger;
    }

    public async Task<SigningKeyRotationResult?> RotateIfDueAsync(CancellationToken cancellationToken)
    {
        return await RotateAsync(force: false, actorUserId: null, cancellationToken: cancellationToken);
    }

    public async Task<SigningKeyRotationResult> RotateNowAsync(Guid? actorUserId, CancellationToken cancellationToken)
    {
        var result = await RotateAsync(force: true, actorUserId: actorUserId, cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Signing key rotation failed.");
    }

    private async Task<SigningKeyRotationResult?> RotateAsync(
        bool force,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var policy = await _dbContext.KeyRotationPolicies
            .FromSqlRaw("SELECT * FROM \"KeyRotationPolicies\" FOR UPDATE")
            .FirstAsync(cancellationToken);

        if (!policy.Enabled && !force)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        if (!force && policy.NextRotationUtc.HasValue && policy.NextRotationUtc.Value > now)
        {
            return null;
        }

        var active = await _keyRingService.GetActiveAsync(cancellationToken);
        if (active is null)
        {
            active = await _keyRingService.EnsureInitializedAsync(cancellationToken);
            policy.LastRotationUtc = active.ActivatedUtc ?? active.CreatedUtc;
            policy.NextRotationUtc = policy.LastRotationUtc.Value.AddDays(Math.Max(1, policy.RotationIntervalDays));
            policy.UpdatedUtc = now;
            policy.UpdatedByUserId = actorUserId;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (force)
            {
                await _incidentService.LogIncidentAsync(
                    "signing_key_initialized",
                    "medium",
                    null,
                    null,
                    new { active.Kid },
                    actorUserId,
                    cancellationToken: cancellationToken);
                return new SigningKeyRotationResult(active, null);
            }

            return null;
        }

        var tokenPolicy = await _tokenPolicyService.GetEffectivePolicyAsync(cancellationToken);
        var safeWindow = CalculateSafeWindow(tokenPolicy, policy.JwksCacheMarginMinutes);
        var graceWindow = TimeSpan.FromDays(Math.Max(0, policy.GracePeriodDays));
        // Retire old keys relative to rotation time so they stay available for the full validation window.
        var effectiveWindow = safeWindow > graceWindow ? safeWindow : graceWindow;

        var rotation = await _keyRingService.RotateNowAsync(effectiveWindow, cancellationToken);
        policy.LastRotationUtc = now;
        policy.NextRotationUtc = now.AddDays(Math.Max(1, policy.RotationIntervalDays));
        policy.UpdatedUtc = now;
        policy.UpdatedByUserId = actorUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _incidentService.LogIncidentAsync(
            "signing_key_rotated",
            "medium",
            null,
            null,
            new { rotation.NewActive.Kid, PreviousKid = rotation.PreviousActive?.Kid },
            actorUserId,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Rotated signing keys. New active {NewKid}, previous {PreviousKid}.",
            rotation.NewActive.Kid,
            rotation.PreviousActive?.Kid);

        return rotation;
    }

    private static TimeSpan CalculateSafeWindow(TokenPolicySnapshot policy, int jwksCacheMarginMinutes)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        var maxLifetimeMinutes = Math.Max(
            policy.AccessTokenMinutes,
            Math.Max(policy.IdentityTokenMinutes, policy.AuthorizationCodeMinutes));
        var marginMinutes = Math.Max(0, jwksCacheMarginMinutes);
        var skewSeconds = Math.Max(0, policy.ClockSkewSeconds);

        // Include max token lifetime plus JWKS cache margin and clock skew since validation allows skewed tokens.
        return TimeSpan.FromMinutes(maxLifetimeMinutes + marginMinutes)
            .Add(TimeSpan.FromSeconds(skewSeconds));
    }
}
