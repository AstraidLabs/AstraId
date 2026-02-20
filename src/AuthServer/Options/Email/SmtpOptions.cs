namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for smtp.
/// </summary>
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
