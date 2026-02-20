namespace AppServer.Security;

/// <summary>
/// Provides configuration options for security hardening.
/// </summary>
public sealed class SecurityHardeningOptions
{
    public const string SectionName = "SecurityHardening";

    public bool Enabled { get; set; } = true;
    public HeadersOptions Headers { get; set; } = new();

    /// <summary>
    /// Provides configuration options for headers.
    /// </summary>
    public sealed class HeadersOptions
    {
        public bool Enabled { get; set; } = true;
    }
}
