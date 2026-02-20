using System.Text.Json;
using AuthServer.Data;
using AuthServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Governance;

/// <summary>
/// Provides o auth advanced policy provider functionality.
/// </summary>
public sealed class OAuthAdvancedPolicyProvider : IOAuthAdvancedPolicyProvider
{
    private const string CacheKey = "governance:oauth-advanced-policy";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _memoryCache;
    private readonly IOptionsMonitor<OAuthAdvancedPolicyDefaultsOptions> _defaults;

    public OAuthAdvancedPolicyProvider(ApplicationDbContext db, IMemoryCache memoryCache, IOptionsMonitor<OAuthAdvancedPolicyDefaultsOptions> defaults)
    {
        _db = db;
        _memoryCache = memoryCache;
        _defaults = defaults;
    }

    public async Task<OAuthAdvancedPolicySnapshot> GetCurrentAsync(CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(CacheKey, out OAuthAdvancedPolicySnapshot? cached) && cached is not null)
        {
            return cached;
        }

        var entity = await _db.OAuthAdvancedPolicies.FirstOrDefaultAsync(cancellationToken)
            ?? await CreateDefaultAsync(cancellationToken);
        var snapshot = ToSnapshot(entity);
        _memoryCache.Set(CacheKey, snapshot, CacheTtl);
        return snapshot;
    }

    public async Task<OAuthAdvancedPolicySnapshot> UpdateAsync(OAuthAdvancedPolicySnapshot snapshot, string rowVersion, Guid? actorUserId, string? actorIp, CancellationToken cancellationToken)
    {
        var entity = await _db.OAuthAdvancedPolicies.FirstOrDefaultAsync(cancellationToken)
            ?? await CreateDefaultAsync(cancellationToken);

        var expectedRowVersion = Convert.FromBase64String(rowVersion);
        _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;

        entity.DeviceFlowEnabled = snapshot.DeviceFlowEnabled;
        entity.DeviceFlowUserCodeTtlMinutes = snapshot.DeviceFlowUserCodeTtlMinutes;
        entity.DeviceFlowPollingIntervalSeconds = snapshot.DeviceFlowPollingIntervalSeconds;
        entity.TokenExchangeEnabled = snapshot.TokenExchangeEnabled;
        entity.TokenExchangeAllowedClientIdsJson = JsonSerializer.Serialize(snapshot.TokenExchangeAllowedClientIds);
        entity.TokenExchangeAllowedAudiencesJson = JsonSerializer.Serialize(snapshot.TokenExchangeAllowedAudiences);
        entity.RefreshRotationEnabled = snapshot.RefreshRotationEnabled;
        entity.RefreshReuseDetectionEnabled = snapshot.RefreshReuseDetectionEnabled;
        entity.RefreshReuseAction = snapshot.RefreshReuseAction;
        entity.BackChannelLogoutEnabled = snapshot.BackChannelLogoutEnabled;
        entity.FrontChannelLogoutEnabled = snapshot.FrontChannelLogoutEnabled;
        entity.LogoutTokenTtlMinutes = snapshot.LogoutTokenTtlMinutes;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        entity.UpdatedByIp = actorIp;

        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return ToSnapshot(entity);
    }

    public void InvalidateCache() => _memoryCache.Remove(CacheKey);

    private async Task<OAuthAdvancedPolicy> CreateDefaultAsync(CancellationToken cancellationToken)
    {
        var defaults = _defaults.CurrentValue;
        var entity = new OAuthAdvancedPolicy
        {
            Id = Guid.NewGuid(),
            DeviceFlowEnabled = defaults.DeviceFlowEnabled,
            DeviceFlowUserCodeTtlMinutes = defaults.DeviceFlowUserCodeTtlMinutes,
            DeviceFlowPollingIntervalSeconds = defaults.DeviceFlowPollingIntervalSeconds,
            TokenExchangeEnabled = defaults.TokenExchangeEnabled,
            TokenExchangeAllowedClientIdsJson = JsonSerializer.Serialize(defaults.TokenExchangeAllowedClientIds),
            TokenExchangeAllowedAudiencesJson = JsonSerializer.Serialize(defaults.TokenExchangeAllowedAudiences),
            RefreshRotationEnabled = defaults.RefreshRotationEnabled,
            RefreshReuseDetectionEnabled = defaults.RefreshReuseDetectionEnabled,
            RefreshReuseAction = defaults.RefreshReuseAction,
            BackChannelLogoutEnabled = defaults.BackChannelLogoutEnabled,
            FrontChannelLogoutEnabled = defaults.FrontChannelLogoutEnabled,
            LogoutTokenTtlMinutes = defaults.LogoutTokenTtlMinutes,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.OAuthAdvancedPolicies.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static OAuthAdvancedPolicySnapshot ToSnapshot(OAuthAdvancedPolicy entity)
    {
        return new OAuthAdvancedPolicySnapshot(
            entity.DeviceFlowEnabled,
            entity.DeviceFlowUserCodeTtlMinutes,
            entity.DeviceFlowPollingIntervalSeconds,
            entity.TokenExchangeEnabled,
            ParseList(entity.TokenExchangeAllowedClientIdsJson),
            ParseList(entity.TokenExchangeAllowedAudiencesJson),
            entity.RefreshRotationEnabled,
            entity.RefreshReuseDetectionEnabled,
            entity.RefreshReuseAction,
            entity.BackChannelLogoutEnabled,
            entity.FrontChannelLogoutEnabled,
            entity.LogoutTokenTtlMinutes,
            entity.UpdatedAtUtc,
            entity.UpdatedByUserId,
            entity.UpdatedByIp,
            Convert.ToBase64String(entity.RowVersion));
    }

    private static IReadOnlyList<string> ParseList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<string[]>(json) ?? [];
    }
}
