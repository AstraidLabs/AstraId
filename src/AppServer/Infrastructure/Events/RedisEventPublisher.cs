using System.Text.Json;
using AstraId.Contracts;
using StackExchange.Redis;

namespace AppServer.Infrastructure.Events;

public sealed class RedisEventPublisher : IEventPublisher
{
    private readonly IConnectionMultiplexer _redis;

    public RedisEventPublisher(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task PublishAsync(AppEvent appEvent, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(appEvent);
        return _redis.GetSubscriber().PublishAsync(EventChannels.AppEvents, payload);
    }
}
