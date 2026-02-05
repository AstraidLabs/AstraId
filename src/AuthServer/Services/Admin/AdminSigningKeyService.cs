using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Governance;
using AuthServer.Services.SigningKeys;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace AuthServer.Services.Admin;

public sealed class AdminSigningKeyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SigningKeyRingService _keyRingService;
    private readonly SigningKeyRotationCoordinator _rotationCoordinator;
    private readonly TokenIncidentService _incidentService;
    private readonly IOptionsMonitorCache<OpenIddictServerOptions> _optionsCache;
    private readonly ISigningKeyRotationState _rotationState;

    public AdminSigningKeyService(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        SigningKeyRingService keyRingService,
        SigningKeyRotationCoordinator rotationCoordinator,
        TokenIncidentService incidentService,
        IOptionsMonitorCache<OpenIddictServerOptions> optionsCache,
        ISigningKeyRotationState rotationState)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _keyRingService = keyRingService;
        _rotationCoordinator = rotationCoordinator;
        _incidentService = incidentService;
        _optionsCache = optionsCache;
        _rotationState = rotationState;
    }

    public async Task<SigningKeyRotationResult> RotateNowAsync(CancellationToken cancellationToken)
    {
        var result = await _rotationCoordinator.RotateNowAsync(GetActorUserId(), cancellationToken);
        _optionsCache.TryRemove(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        _rotationState.LastRotationUtc = DateTimeOffset.UtcNow;
        await LogAuditAsync("signing-keys.rotated", "SigningKeyRing", result.NewActive.Kid, new
        {
            newKid = result.NewActive.Kid,
            previousKid = result.PreviousActive?.Kid
        });
        return result;
    }

    public async Task<SigningKeyRingEntry?> RetireAsync(string kid, CancellationToken cancellationToken)
    {
        var entry = await _keyRingService.RetireAsync(kid, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        _optionsCache.TryRemove(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        await LogAuditAsync("signing-keys.retired", "SigningKeyRing", entry.Kid, new
        {
            kid = entry.Kid,
            status = entry.Status.ToString()
        });

        await _incidentService.LogIncidentAsync(
            "signing_key_retired",
            "medium",
            null,
            null,
            new { entry.Kid },
            GetActorUserId(),
            cancellationToken: cancellationToken);

        return entry;
    }

    public async Task<SigningKeyRevokeResult> RevokeAsync(string kid, CancellationToken cancellationToken)
    {
        var result = await _keyRingService.RevokeAsync(kid, cancellationToken);
        if (result != SigningKeyRevokeResult.NotFound)
        {
            _optionsCache.TryRemove(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            await LogAuditAsync("signing-keys.revoked", "SigningKeyRing", kid, new
            {
                kid,
                result = result.ToString()
            });

            await _incidentService.LogIncidentAsync(
                "signing_key_revoked",
                "high",
                null,
                null,
                new { kid, result = result.ToString() },
                GetActorUserId(),
                cancellationToken: cancellationToken);
        }

        return result;
    }

    private async Task LogAuditAsync(string action, string targetType, string targetId, object data)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = GetActorUserId(),
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = JsonSerializer.Serialize(data)
        });

        await _dbContext.SaveChangesAsync();
    }

    private Guid? GetActorUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }
}
