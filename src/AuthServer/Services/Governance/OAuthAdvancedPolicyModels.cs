using AuthServer.Data;

namespace AuthServer.Services.Governance;

public sealed record OAuthAdvancedPolicySnapshot(
    bool DeviceFlowEnabled,
    int DeviceFlowUserCodeTtlMinutes,
    int DeviceFlowPollingIntervalSeconds,
    bool TokenExchangeEnabled,
    IReadOnlyList<string> TokenExchangeAllowedClientIds,
    IReadOnlyList<string> TokenExchangeAllowedAudiences,
    bool RefreshRotationEnabled,
    bool RefreshReuseDetectionEnabled,
    RefreshReuseAction RefreshReuseAction,
    bool BackChannelLogoutEnabled,
    bool FrontChannelLogoutEnabled,
    int LogoutTokenTtlMinutes,
    DateTime UpdatedAtUtc,
    Guid? UpdatedByUserId,
    string? UpdatedByIp,
    string RowVersion);
