namespace AuthServer.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Mode { get; set; } = "Smtp";

    public string FromEmail { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public SmtpOptions Smtp { get; set; } = new();

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
}
