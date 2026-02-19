namespace AppServer.Security;

public sealed class InternalTokenOptions
{
    public const string SectionName = "InternalTokens";

    public string Issuer { get; set; } = "astraid-api";
    public string Audience { get; set; } = "astraid-app";
    public string[] AllowedServices { get; set; } = ["api"];
    public string[] AllowedAlgorithms { get; set; } = ["RS256"];
    public string JwksUrl { get; set; } = "https://localhost:7002/internal/.well-known/jwks.json";
    public int JwksRefreshMinutes { get; set; } = 5;
    public string JwksInternalApiKey { get; set; } = "__REPLACE_ME__";
    public bool AllowLegacyHs256 { get; set; }
    public string LegacyHs256Secret { get; set; } = "__REPLACE_ME__";
}

public sealed class AppServerMtlsOptions
{
    public const string SectionName = "AppServer:Mtls";

    public bool Enabled { get; set; }
    public bool RequireClientCertificate { get; set; }
    public string[] AllowedClientThumbprints { get; set; } = [];
    public string[] AllowedClientSubjectNames { get; set; } = [];
}
