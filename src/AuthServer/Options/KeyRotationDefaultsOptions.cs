namespace AuthServer.Options;

public sealed class KeyRotationDefaultsOptions
{
    public const string SectionName = "AuthServer:KeyRotationDefaults";

    public bool Enabled { get; set; } = true;
    public int RotationIntervalDays { get; set; } = 30;
    public int GracePeriodDays { get; set; } = 14;
}
