namespace Api.Options;

/// <summary>
/// Provides configuration options for app server.
/// </summary>
public sealed class AppServerOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public MtlsOptions Mtls { get; set; } = new();
}

/// <summary>
/// Provides configuration options for mtls.
/// </summary>
public sealed class MtlsOptions
{
    public bool Enabled { get; set; }
    public MtlsClientCertificateOptions ClientCertificate { get; set; } = new();
    public MtlsServerCertificateOptions ServerCertificate { get; set; } = new();
}

/// <summary>
/// Provides configuration options for mtls client certificate.
/// </summary>
public sealed class MtlsClientCertificateOptions
{
    public string Source { get; set; } = "File";
    public string Path { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Thumbprint { get; set; } = string.Empty;
}

/// <summary>
/// Provides configuration options for mtls server certificate.
/// </summary>
public sealed class MtlsServerCertificateOptions
{
    public string ValidationMode { get; set; } = "System";
    public string[] PinnedThumbprints { get; set; } = [];
}
