namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for key rotation defaults.
/// </summary>
public sealed class KeyRotationDefaultsOptions
{
    public const string SectionName = "AuthServer:KeyRotationDefaults";

    public bool Enabled { get; set; } = true;
    public int RotationIntervalDays { get; set; } = 30;
    public int GracePeriodDays { get; set; } = 14;
    public int JwksCacheMarginMinutes { get; set; } = 60;
}
