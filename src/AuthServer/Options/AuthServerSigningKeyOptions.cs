namespace AuthServer.Options;

public sealed class AuthServerSigningKeyOptions
{
    public const string SectionName = "AuthServer:SigningKeys";

    public SigningKeyMode Mode { get; set; } = SigningKeyMode.Auto;
    public bool Enabled { get; set; } = true;
    public int RotationIntervalDays { get; set; } = 30;
    public int PreviousKeyRetentionDays { get; set; } = 14;
    public int CheckPeriodMinutes { get; set; } = 60;
    public string Algorithm { get; set; } = "RS256";
    public int KeySize { get; set; } = 2048;
}

public enum SigningKeyMode
{
    Auto = 0,
    Certificates = 1,
    DbKeyRing = 2
}
