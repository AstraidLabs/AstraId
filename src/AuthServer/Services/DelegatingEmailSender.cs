namespace AuthServer.Services;

public sealed class DelegatingEmailSender : IEmailSender
{
    private readonly EmailSenderFactory _factory;

    public DelegatingEmailSender(EmailSenderFactory factory)
    {
        _factory = factory;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        => _factory.GetSender().SendAsync(message, cancellationToken);
}
