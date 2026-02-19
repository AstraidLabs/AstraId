using AuthServer.Data;

namespace AuthServer.Options;

public sealed class OAuthAdvancedPolicyDefaultsOptions
{
    public const string SectionName = "AuthServer:OAuthAdvancedPolicyDefaults";

    public bool DeviceFlowEnabled { get; set; } = false;
    public int DeviceFlowUserCodeTtlMinutes { get; set; } = 10;
    public int DeviceFlowPollingIntervalSeconds { get; set; } = 5;
    public bool TokenExchangeEnabled { get; set; } = false;
    public string[] TokenExchangeAllowedClientIds { get; set; } = [];
    public string[] TokenExchangeAllowedAudiences { get; set; } = [];
    public bool RefreshRotationEnabled { get; set; } = false;
    public bool RefreshReuseDetectionEnabled { get; set; } = false;
    public RefreshReuseAction RefreshReuseAction { get; set; } = RefreshReuseAction.RevokeFamily;
    public bool BackChannelLogoutEnabled { get; set; } = false;
    public bool FrontChannelLogoutEnabled { get; set; } = false;
    public int LogoutTokenTtlMinutes { get; set; } = 5;
}
