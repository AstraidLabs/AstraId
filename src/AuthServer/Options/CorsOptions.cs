namespace AuthServer.Options;

/// <summary>
/// Browser CORS policy used by the authorization server endpoints that are called from web-based clients.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    // Origins must be full scheme+host(+port) values; wildcard origins are blocked by startup validation.
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    // Credentials should only be enabled when AllowedOrigins is explicit to avoid cross-site cookie leakage.
    public bool AllowCredentials { get; set; } = true;
}
