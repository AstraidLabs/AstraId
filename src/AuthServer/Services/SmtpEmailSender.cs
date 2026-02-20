using AuthServer.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AuthServer.Services;

/// <summary>
/// Provides smtp email sender functionality.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var smtp = options.Smtp;

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(options.FromName ?? string.Empty, options.FromEmail));
        email.To.Add(string.IsNullOrWhiteSpace(message.ToName)
            ? MailboxAddress.Parse(message.ToEmail)
            : new MailboxAddress(message.ToName, message.ToEmail));
        email.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        };
        email.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = Math.Max(smtp.TimeoutSeconds, 1) * 1000;

        var socketOptions = smtp.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : smtp.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.StartTlsWhenAvailable;

        if (!smtp.UseSsl && !smtp.UseStartTls)
        {
            _logger.LogWarning("SMTP is not configured for TLS; using STARTTLS when available.");
        }

        _logger.LogInformation(
            "Sending email via {Host}:{Port}.",
            smtp.Host,
            smtp.Port);

        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        }

        await client.SendAsync(email, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
