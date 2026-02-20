namespace AuthServer.Options;

/// <summary>
/// Device authorization controls for constrained-input clients that rely on the device verification endpoint.
/// </summary>
public sealed class AuthServerDeviceFlowOptions
{
    public const string SectionName = "AuthServer:DeviceFlow";

    public bool Enabled { get; set; } = false;
    // Limits are requests per minute and should remain strict because these endpoints are internet-facing.
    public int VerificationRateLimitPerMinute { get; set; } = 10;
    public int PollingRateLimitPerMinute { get; set; } = 30;
}
