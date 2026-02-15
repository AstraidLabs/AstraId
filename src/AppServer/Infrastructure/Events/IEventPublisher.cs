using AstraId.Contracts;

namespace AppServer.Infrastructure.Events;

public interface IEventPublisher
{
    Task PublishAsync(AppEvent appEvent, CancellationToken cancellationToken = default);
}
