using AstraId.Contracts;

namespace AuthServer.Services.Events;

/// <summary>
/// Provides in memory event publisher functionality.
/// </summary>
public sealed class InMemoryEventPublisher : IEventPublisher
{
    private readonly ILogger<InMemoryEventPublisher> _logger;

    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(AppEvent appEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Skipping distributed event publish in development fallback. Event: {EventType}",
            appEvent.Type);
        return Task.CompletedTask;
    }
}
