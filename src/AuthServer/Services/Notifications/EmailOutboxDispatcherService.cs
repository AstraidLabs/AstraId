using AuthServer.Data;
using AuthServer.Options;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Notifications;

public sealed class EmailOutboxDispatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailOutboxDispatcherService> _logger;
    private readonly IOptions<NotificationOptions> _options;

    public EmailOutboxDispatcherService(IServiceScopeFactory scopeFactory, ILogger<EmailOutboxDispatcherService> logger, IOptions<NotificationOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email outbox dispatcher run failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.Value.DispatcherIntervalSeconds)), stoppingToken);
        }
    }

    private async Task DispatchOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var outbox = scope.ServiceProvider.GetRequiredService<EmailOutboxService>();
        var pending = await outbox.GetPendingDueAsync(Math.Max(1, _options.Value.DispatcherBatchSize), cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                message.LastAttemptUtc = DateTime.UtcNow;
                message.Attempts += 1;
                await sender.SendAsync(new EmailMessage(message.ToEmail, message.ToName, message.Subject, message.HtmlBody, message.TextBody), cancellationToken);
                message.Status = EmailOutboxStatus.Sent;
                message.Error = null;
            }
            catch (Exception ex)
            {
                var delay = ComputeBackoff(message.Attempts);
                message.Error = ex.Message.Length > 3900 ? ex.Message[..3900] : ex.Message;
                message.NextAttemptUtc = DateTime.UtcNow.Add(delay);
                message.Status = message.Attempts >= message.MaxAttempts ? EmailOutboxStatus.Failed : EmailOutboxStatus.Pending;
                _logger.LogWarning(ex, "Failed to send outbox email {EmailOutboxId} ({Type}) trace={TraceId} correlation={CorrelationId}.", message.Id, message.Type, message.TraceId, message.CorrelationId);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private TimeSpan ComputeBackoff(int attempts)
    {
        var baseSeconds = Math.Max(1, _options.Value.BaseBackoffSeconds);
        var cap = Math.Max(baseSeconds, _options.Value.MaxBackoffSeconds);
        var next = Math.Min(cap, baseSeconds * Math.Pow(2, Math.Max(0, attempts - 1)));
        return TimeSpan.FromSeconds(next);
    }
}
