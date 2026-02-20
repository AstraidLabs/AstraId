namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for security hardening.
/// </summary>
public sealed class SecurityHardeningOptions
{
    public const string SectionName = "SecurityHardening";

    public bool Enabled { get; set; } = true;
    public RateLimitingOptions RateLimiting { get; set; } = new();
    public HeadersOptions Headers { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();

    /// <summary>
    /// Provides configuration options for rate limiting.
    /// </summary>
    public sealed class RateLimitingOptions
    {
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Provides configuration options for headers.
    /// </summary>
    public sealed class HeadersOptions
    {
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Provides configuration options for cors.
    /// </summary>
    public sealed class CorsOptions
    {
        public bool StrictMode { get; set; } = true;
    }
}
