using System.Text.Json;
using AstraId.Contracts;
using Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace Api.Events;

/// <summary>
/// Provides redis event subscriber functionality.
/// </summary>
public sealed class RedisEventSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<AppHub> _hubContext;
    private readonly ILogger<RedisEventSubscriber> _logger;

    public RedisEventSubscriber(
        IConnectionMultiplexer redis,
        IHubContext<AppHub> hubContext,
        ILogger<RedisEventSubscriber> logger)
    {
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Subscribes to configured Redis channels and forwards events to SignalR clients.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Reuse the Redis pub/sub subscriber to receive app events from the shared channel.
        var subscriber = _redis.GetSubscriber();
        await subscriber.SubscribeAsync(EventChannels.AppEvents, async (_, value) =>
        {
            try
            {
                var appEvent = JsonSerializer.Deserialize<AppEvent>(value.ToString());
                // Ignore payloads that cannot be deserialized into the app event contract.
                if (appEvent is null)
                {
                    return;
                }

                var eventName = $"app.{appEvent.Type}";
                // Fan out user-scoped events to the matching user group.
                if (!string.IsNullOrWhiteSpace(appEvent.UserId))
                {
                    await _hubContext.Clients.Group($"user:{appEvent.UserId}").SendAsync(eventName, appEvent, cancellationToken: stoppingToken);
                }

                // Fan out tenant-scoped events to all clients in the tenant group.
                if (!string.IsNullOrWhiteSpace(appEvent.TenantId))
                {
                    await _hubContext.Clients.Group($"tenant:{appEvent.TenantId}").SendAsync(eventName, appEvent, cancellationToken: stoppingToken);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to process event from Redis channel {Channel}.", EventChannels.AppEvents);
            }
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
