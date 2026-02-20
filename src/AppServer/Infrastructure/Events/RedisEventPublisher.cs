using System.Text.Json;
using AstraId.Contracts;
using StackExchange.Redis;

namespace AppServer.Infrastructure.Events;

/// <summary>
/// Provides redis event publisher functionality.
/// </summary>
public sealed class RedisEventPublisher : IEventPublisher
{
    private readonly IConnectionMultiplexer _redis;

    public RedisEventPublisher(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task PublishAsync(AppEvent appEvent, CancellationToken cancellationToken = default)
    {
        // Serialize the event contract to JSON before publishing to the Redis channel.
        var payload = JsonSerializer.Serialize(appEvent);
        return _redis.GetSubscriber().PublishAsync(EventChannels.AppEvents, payload);
    }
}
