using AuthServer.Data;

namespace AuthServer.Services.Admin.Models;

public sealed record AdminOAuthAdvancedPolicyResponse(AdminOAuthAdvancedPolicy Policy);

public sealed record AdminOAuthAdvancedPolicy(
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

public sealed record UpdateAdminOAuthAdvancedPolicyRequest(
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
    string RowVersion,
    bool BreakGlass = false);
