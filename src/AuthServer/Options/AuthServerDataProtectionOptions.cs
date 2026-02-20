namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for auth server data protection.
/// </summary>
public sealed class AuthServerDataProtectionOptions
{
    public const string SectionName = "AuthServer:DataProtection";

    public string? KeyPath { get; set; }
    public bool ReadOnly { get; set; }
}
