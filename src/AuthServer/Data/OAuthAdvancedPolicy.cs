namespace AuthServer.Data;

public enum RefreshReuseAction
{
    RevokeFamily = 0,
    RevokeAllSessions = 1,
    IncidentOnly = 2
}

public sealed class OAuthAdvancedPolicy
{
    public Guid Id { get; set; }
    public bool DeviceFlowEnabled { get; set; }
    public int DeviceFlowUserCodeTtlMinutes { get; set; }
    public int DeviceFlowPollingIntervalSeconds { get; set; }
    public bool TokenExchangeEnabled { get; set; }
    public string TokenExchangeAllowedClientIdsJson { get; set; } = "[]";
    public string TokenExchangeAllowedAudiencesJson { get; set; } = "[]";
    public bool RefreshRotationEnabled { get; set; }
    public bool RefreshReuseDetectionEnabled { get; set; }
    public RefreshReuseAction RefreshReuseAction { get; set; }
    public bool BackChannelLogoutEnabled { get; set; }
    public bool FrontChannelLogoutEnabled { get; set; }
    public int LogoutTokenTtlMinutes { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? UpdatedByIp { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
