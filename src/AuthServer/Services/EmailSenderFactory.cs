using AuthServer.Options;
using Microsoft.Extensions.Options;

namespace AuthServer.Services;

/// <summary>
/// Provides email sender factory functionality.
/// </summary>
public sealed class EmailSenderFactory
{
    private readonly IServiceProvider _services;
    private readonly IOptions<EmailOptions> _options;

    public EmailSenderFactory(IServiceProvider services, IOptions<EmailOptions> options)
    {
        _services = services;
        _options = options;
    }

    public IEmailSender GetSender()
    {
        var provider = _options.Value.GetProviderOrDefault();

        if (provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase)) return _services.GetRequiredService<SendGridEmailSender>();
        if (provider.Equals("Mailgun", StringComparison.OrdinalIgnoreCase)) return _services.GetRequiredService<MailgunEmailSender>();
        if (provider.Equals("Postmark", StringComparison.OrdinalIgnoreCase)) return _services.GetRequiredService<PostmarkEmailSender>();
        if (provider.Equals("Graph", StringComparison.OrdinalIgnoreCase)) return _services.GetRequiredService<GraphEmailSender>();
        return _services.GetRequiredService<SmtpEmailSender>();
    }
}
