using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.SigningKeys;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace AuthServer.Services.Admin;

public sealed class AdminSigningKeyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SigningKeyRingService _keyRingService;
    private readonly IOptionsMonitorCache<OpenIddictServerOptions> _optionsCache;
    private readonly ISigningKeyRotationState _rotationState;

    public AdminSigningKeyService(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        SigningKeyRingService keyRingService,
        IOptionsMonitorCache<OpenIddictServerOptions> optionsCache,
        ISigningKeyRotationState rotationState)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _keyRingService = keyRingService;
        _optionsCache = optionsCache;
        _rotationState = rotationState;
    }

    public async Task<SigningKeyRotationResult> RotateNowAsync(CancellationToken cancellationToken)
    {
        var result = await _keyRingService.RotateNowAsync(cancellationToken);
        _optionsCache.TryRemove(OpenIddictServerDefaults.AuthenticationScheme);
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

        _optionsCache.TryRemove(OpenIddictServerDefaults.AuthenticationScheme);
        await LogAuditAsync("signing-keys.retired", "SigningKeyRing", entry.Kid, new
        {
            kid = entry.Kid,
            status = entry.Status.ToString()
        });

        return entry;
    }

    public async Task<SigningKeyRevokeResult> RevokeAsync(string kid, CancellationToken cancellationToken)
    {
        var result = await _keyRingService.RevokeAsync(kid, cancellationToken);
        if (result != SigningKeyRevokeResult.NotFound)
        {
            _optionsCache.TryRemove(OpenIddictServerDefaults.AuthenticationScheme);
            await LogAuditAsync("signing-keys.revoked", "SigningKeyRing", kid, new
            {
                kid,
                result = result.ToString()
            });
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
