namespace AppServer.Security;

public sealed class SecurityHardeningOptions
{
    public const string SectionName = "SecurityHardening";

    public bool Enabled { get; set; } = true;
    public HeadersOptions Headers { get; set; } = new();

    public sealed class HeadersOptions
    {
        public bool Enabled { get; set; } = true;
    }
}
