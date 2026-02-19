namespace AuthServer.Options;

public sealed class SecurityDiagnosticsOptions
{
    public const string SectionName = "SecurityDiagnostics";

    public bool EnableMfaTokenProtectionEndpoint { get; set; }
    public bool EnableOpenIddictSecretStorageEndpoint { get; set; }
}
