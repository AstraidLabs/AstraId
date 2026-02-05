namespace AuthServer.Options;

public sealed class AuthServerDataProtectionOptions
{
    public const string SectionName = "AuthServer:DataProtection";

    public string? KeyPath { get; set; }
    public bool ReadOnly { get; set; }
}
