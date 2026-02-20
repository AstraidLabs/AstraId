using System.Text.Json;
using AstraId.Contracts;
using Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace Api.Events;

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
        var subscriber = _redis.GetSubscriber();
        await subscriber.SubscribeAsync(EventChannels.AppEvents, async (_, value) =>
        {
            try
            {
                var appEvent = JsonSerializer.Deserialize<AppEvent>(value.ToString());
                if (appEvent is null)
                {
                    return;
                }

                var eventName = $"app.{appEvent.Type}";
                if (!string.IsNullOrWhiteSpace(appEvent.UserId))
                {
                    await _hubContext.Clients.Group($"user:{appEvent.UserId}").SendAsync(eventName, appEvent, cancellationToken: stoppingToken);
                }

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
