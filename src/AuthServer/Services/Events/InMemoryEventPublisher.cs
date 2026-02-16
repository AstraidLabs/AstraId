namespace AuthServer.Services.Events;

public sealed class InMemoryEventPublisher : IEventPublisher
{
    private readonly ILogger<InMemoryEventPublisher> _logger;

    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string channel, string payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Skipping distributed event publish in development fallback. Channel: {Channel}", channel);
        return Task.CompletedTask;
    }
}
