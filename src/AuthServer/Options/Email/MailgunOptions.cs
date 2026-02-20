namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for mailgun.
/// </summary>
public sealed class MailgunOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.mailgun.net";
}
