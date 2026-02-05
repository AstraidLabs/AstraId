using System.Text.Json;
using AuthServer.Data;

namespace AuthServer.Services.Governance;

public sealed class TokenIncidentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenIncidentService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogIncidentAsync(
        string type,
        string severity,
        Guid? userId,
        string? clientId,
        object? detail,
        Guid? actorUserId = null,
        string? traceId = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedTraceId = traceId ?? _httpContextAccessor.HttpContext?.TraceIdentifier;
        var incident = new TokenIncident
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            Type = type,
            Severity = severity,
            UserId = userId,
            ClientId = clientId,
            TraceId = resolvedTraceId,
            DetailJson = detail is null ? null : JsonSerializer.Serialize(detail),
            ActorUserId = actorUserId
        };

        _dbContext.TokenIncidents.Add(incident);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
