namespace AuthServer.Options;

/// <summary>
/// Configuration for issuer signing-key generation and rotation used by OpenID Connect/JWT token issuance.
/// </summary>
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

/// <summary>
/// Selects where signing keys come from: automatic generation, certificates, or the database-backed key ring.
/// </summary>
public enum SigningKeyMode
{
    Auto = 0,
    Certificates = 1,
    DbKeyRing = 2
}
