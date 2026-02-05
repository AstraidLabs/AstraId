using AuthServer.Data;
using AuthServer.Services.Governance;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.SigningKeys;

public sealed class SigningKeyRotationCoordinator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SigningKeyRingService _keyRingService;
    private readonly TokenIncidentService _incidentService;
    private readonly ILogger<SigningKeyRotationCoordinator> _logger;

    public SigningKeyRotationCoordinator(
        ApplicationDbContext dbContext,
        SigningKeyRingService keyRingService,
        TokenIncidentService incidentService,
        ILogger<SigningKeyRotationCoordinator> logger)
    {
        _dbContext = dbContext;
        _keyRingService = keyRingService;
        _incidentService = incidentService;
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

        var rotation = await _keyRingService.RotateNowAsync(policy.GracePeriodDays, cancellationToken);
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
}
