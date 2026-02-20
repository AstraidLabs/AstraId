using AstraId.Contracts;

namespace AuthServer.Services.Events;

/// <summary>
/// Defines the contract for event publisher.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(AppEvent appEvent, CancellationToken cancellationToken = default);
}
