namespace AuthServer.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = string.Empty;

    // Backward compatibility for existing configuration key Email:Mode.
    public string? Mode { get; set; }

    public string FromEmail { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public SmtpOptions Smtp { get; set; } = new();

    public SendGridOptions SendGrid { get; set; } = new();

    public string GetProviderOrDefault()
    {
        if (!string.IsNullOrWhiteSpace(Provider))
        {
            return Provider;
        }

        if (!string.IsNullOrWhiteSpace(Mode))
        {
            return Mode;
        }

        return "Smtp";
    }

    public sealed class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public bool UseSsl { get; set; }

        public bool UseStartTls { get; set; }

        public int TimeoutSeconds { get; set; } = 10;
    }

    public sealed class SendGridOptions
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}
