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

    public AdminSigningKeyService(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        SigningKeyRingService keyRingService,
        IOptionsMonitorCache<OpenIddictServerOptions> optionsCache)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _keyRingService = keyRingService;
        _optionsCache = optionsCache;
    }

    public async Task<SigningKeyRotationResult> RotateNowAsync(CancellationToken cancellationToken)
    {
        var result = await _keyRingService.RotateNowAsync(cancellationToken);
        _optionsCache.TryRemove(OpenIddictServerDefaults.AuthenticationScheme);
        await LogAuditAsync("signing-keys.rotated", "SigningKeyRing", result.NewActive.Kid, new
        {
            newKid = result.NewActive.Kid,
            previousKid = result.PreviousActive?.Kid
        });
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
