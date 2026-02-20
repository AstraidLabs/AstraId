namespace AuthServer.Services;

/// <summary>
/// Defines the contract for email sender.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
