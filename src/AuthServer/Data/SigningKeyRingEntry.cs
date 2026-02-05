namespace AuthServer.Data;

public sealed class SigningKeyRingEntry
{
    public Guid Id { get; set; }
    public string Kid { get; set; } = string.Empty;
    public SigningKeyStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ActivatedUtc { get; set; }
    public DateTime? RetireAfterUtc { get; set; }
    public DateTime? RetiredUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public DateTime? NotBeforeUtc { get; set; }
    public DateTime? NotAfterUtc { get; set; }
    public string Algorithm { get; set; } = "RS256";
    public string KeyType { get; set; } = "RSA";
    public string PublicJwkJson { get; set; } = string.Empty;
    public string PrivateKeyProtected { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}

public enum SigningKeyStatus
{
    Active = 0,
    Previous = 1,
    Retired = 2,
    Revoked = 3
}
