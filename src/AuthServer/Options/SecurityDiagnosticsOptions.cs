namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for security diagnostics.
/// </summary>
public sealed class SecurityDiagnosticsOptions
{
    public const string SectionName = "SecurityDiagnostics";

    public bool EnableMfaTokenProtectionEndpoint { get; set; }
    public bool EnableOpenIddictSecretStorageEndpoint { get; set; }
}
