namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for auth server certificate.
/// </summary>
public sealed class AuthServerCertificateOptions
{
    public const string SectionName = "AuthServer:Certificates";

    public CertificateDescriptor? Signing { get; set; }
    public CertificateDescriptor? Encryption { get; set; }
}

/// <summary>
/// Provides certificate descriptor functionality.
/// </summary>
public sealed class CertificateDescriptor
{
    public string? Path { get; set; }
    public string? Password { get; set; }
    public string? Base64 { get; set; }
    public string? Thumbprint { get; set; }
    public string? StoreName { get; set; }
    public string? StoreLocation { get; set; }
}
