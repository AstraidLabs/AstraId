using AuthServer.Data;
using AuthServer.Services.Events;
using AstraId.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services;

public interface IUserSecurityEventLogger
{
    Task LogAsync(string eventType, Guid? userId, HttpContext httpContext, string? clientId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserSecurityEvent>> GetRecentForUserAsync(Guid userId, int take, CancellationToken cancellationToken);
}

public sealed class UserSecurityEventLogger : IUserSecurityEventLogger
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;

    public UserSecurityEventLogger(ApplicationDbContext dbContext, IEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    public async Task LogAsync(string eventType, Guid? userId, HttpContext httpContext, string? clientId = null, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<UserSecurityEvent>().Add(new UserSecurityEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TimestampUtc = DateTime.UtcNow,
            EventType = eventType,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            ClientId = clientId,
            TraceId = httpContext.TraceIdentifier
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (userId.HasValue)
        {
            await _eventPublisher.PublishAsync(new AppEvent(
                Type: $"auth.{eventType}",
                TenantId: null,
                UserId: userId.Value.ToString("N"),
                EntityId: userId.Value.ToString("N"),
                OccurredAt: DateTimeOffset.UtcNow));
        }
    }

    public async Task<IReadOnlyCollection<UserSecurityEvent>> GetRecentForUserAsync(Guid userId, int take, CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 100);
        return await _dbContext.Set<UserSecurityEvent>()
            .AsNoTracking()
            .Where(evt => evt.UserId == userId)
            .OrderByDescending(evt => evt.TimestampUtc)
            .Take(safeTake)
            .ToListAsync(cancellationToken);
    }
}
