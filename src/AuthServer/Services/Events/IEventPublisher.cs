using AstraId.Contracts;

namespace AuthServer.Services.Events;

public interface IEventPublisher
{
    Task PublishAsync(AppEvent appEvent, CancellationToken cancellationToken = default);
}
