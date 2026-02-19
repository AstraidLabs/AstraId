namespace AuthServer.Options;

public sealed class AuthServerDeviceFlowOptions
{
    public const string SectionName = "AuthServer:DeviceFlow";

    public bool Enabled { get; set; } = false;
    public int VerificationRateLimitPerMinute { get; set; } = 10;
    public int PollingRateLimitPerMinute { get; set; } = 30;
}
