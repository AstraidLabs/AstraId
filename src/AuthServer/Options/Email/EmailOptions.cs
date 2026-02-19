namespace AuthServer.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Smtp";

    // Backward compatibility for existing configuration key Email:Mode.
    public string? Mode { get; set; }

    public string FromEmail { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public SmtpOptions Smtp { get; set; } = new();

    public SendGridOptions SendGrid { get; set; } = new();

    public MailgunOptions Mailgun { get; set; } = new();

    public PostmarkOptions Postmark { get; set; } = new();

    public GraphOptions Graph { get; set; } = new();

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
}
