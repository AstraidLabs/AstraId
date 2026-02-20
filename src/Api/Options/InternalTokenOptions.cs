namespace Api.Options;

/// <summary>
/// Provides configuration options for internal token.
/// </summary>
public sealed class InternalTokenOptions
{
    public const string SectionName = "InternalTokens";

    public string Issuer { get; set; } = "astraid-api";
    public string Audience { get; set; } = "astraid-app";
    public int TokenTtlSeconds { get; set; } = 120;
    public string[] AllowedServices { get; set; } = ["api"];
    public InternalTokenSigningOptions Signing { get; set; } = new();
    public InternalTokenJwksOptions Jwks { get; set; } = new();
}

/// <summary>
/// Provides configuration options for internal token signing.
/// </summary>
public sealed class InternalTokenSigningOptions
{
    public string Algorithm { get; set; } = "RS256";
    public bool RotationEnabled { get; set; } = true;
    public int RotationIntervalDays { get; set; } = 30;
    public int PreviousKeyRetentionDays { get; set; } = 60;
    public int KeySize { get; set; } = 2048;
}

/// <summary>
/// Provides configuration options for internal token jwks.
/// </summary>
public sealed class InternalTokenJwksOptions
{
    public bool Enabled { get; set; } = true;
    public string Path { get; set; } = "/internal/.well-known/jwks.json";
    public bool RequireInternalApiKey { get; set; } = true;
    public string InternalApiKeyHeaderName { get; set; } = "X-Internal-Api-Key";
    public string InternalApiKey { get; set; } = string.Empty;
}
