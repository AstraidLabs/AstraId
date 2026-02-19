namespace AuthServer.Options;

public sealed class SecurityHardeningOptions
{
    public const string SectionName = "SecurityHardening";

    public bool Enabled { get; set; } = true;
    public RateLimitingOptions RateLimiting { get; set; } = new();
    public HeadersOptions Headers { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();

    public sealed class RateLimitingOptions
    {
        public bool Enabled { get; set; } = true;
    }

    public sealed class HeadersOptions
    {
        public bool Enabled { get; set; } = true;
    }

    public sealed class CorsOptions
    {
        public bool StrictMode { get; set; } = true;
    }
}
