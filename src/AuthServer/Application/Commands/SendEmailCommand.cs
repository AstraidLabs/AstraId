using AuthServer.Services;
using AuthServer.Services.Notifications;
using MediatR;

namespace AuthServer.Application.Commands;

public sealed record SendEmailCommand(string ToEmail, string Subject, string Body) : IRequest;

public sealed class SendEmailCommandHandler : IRequestHandler<SendEmailCommand>
{
    private readonly IEmailSender _emailSender;

    public SendEmailCommandHandler(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        var message = new EmailMessage(
            request.ToEmail,
            null,
            request.Subject,
            request.Body);

        return _emailSender.SendAsync(message, cancellationToken);
    }
}
