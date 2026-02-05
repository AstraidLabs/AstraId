using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.Tokens;

namespace AuthServer.Services.Admin;

public sealed class AdminTokenPolicyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenPolicyService _tokenPolicyService;

    public AdminTokenPolicyService(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        TokenPolicyService tokenPolicyService)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _tokenPolicyService = tokenPolicyService;
    }

    public async Task<AdminTokenPolicyConfig> GetConfigAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _tokenPolicyService.GetEffectivePolicyAsync(cancellationToken);
        return ToAdminConfig(snapshot);
    }

    public async Task<AdminTokenPolicyConfig> UpdateConfigAsync(
        AdminTokenPolicyConfig config,
        CancellationToken cancellationToken)
    {
        var before = await _tokenPolicyService.GetEffectivePolicyAsync(cancellationToken);
        var updated = await _tokenPolicyService.UpdateOverridesAsync(ToSnapshot(config), cancellationToken);

        await LogAuditAsync("tokens.config.updated", "TokenPolicy", "global", new
        {
            previous = before,
            current = updated
        });

        return ToAdminConfig(updated);
    }

    private static TokenPolicySnapshot ToSnapshot(AdminTokenPolicyConfig config)
    {
        return new TokenPolicySnapshot(
            new TokenPreset(
                config.Public.AccessTokenMinutes,
                config.Public.IdentityTokenMinutes,
                config.Public.RefreshTokenAbsoluteDays,
                config.Public.RefreshTokenSlidingDays),
            new TokenPreset(
                config.Confidential.AccessTokenMinutes,
                config.Confidential.IdentityTokenMinutes,
                config.Confidential.RefreshTokenAbsoluteDays,
                config.Confidential.RefreshTokenSlidingDays),
            new RefreshTokenPolicy(
                config.RefreshPolicy.RotationEnabled,
                config.RefreshPolicy.ReuseDetectionEnabled,
                config.RefreshPolicy.ReuseLeewaySeconds));
    }

    private static AdminTokenPolicyConfig ToAdminConfig(TokenPolicySnapshot snapshot)
    {
        return new AdminTokenPolicyConfig(
            new AdminTokenPreset(
                snapshot.Public.AccessTokenMinutes,
                snapshot.Public.IdentityTokenMinutes,
                snapshot.Public.RefreshTokenAbsoluteDays,
                snapshot.Public.RefreshTokenSlidingDays),
            new AdminTokenPreset(
                snapshot.Confidential.AccessTokenMinutes,
                snapshot.Confidential.IdentityTokenMinutes,
                snapshot.Confidential.RefreshTokenAbsoluteDays,
                snapshot.Confidential.RefreshTokenSlidingDays),
            new AdminRefreshTokenPolicy(
                snapshot.RefreshPolicy.RotationEnabled,
                snapshot.RefreshPolicy.ReuseDetectionEnabled,
                snapshot.RefreshPolicy.ReuseLeewaySeconds));
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
