using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Governance;

namespace AuthServer.Services;

public sealed class UserSessionRevocationService
{
    private readonly TokenRevocationService _tokenRevocationService;
    private readonly ApplicationDbContext _dbContext;

    public UserSessionRevocationService(TokenRevocationService tokenRevocationService, ApplicationDbContext dbContext)
    {
        _tokenRevocationService = tokenRevocationService;
        _dbContext = dbContext;
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        string? auditAction,
        HttpContext httpContext,
        object? metadata,
        CancellationToken cancellationToken)
    {
        var result = await _tokenRevocationService.RevokeUserAsync(userId, cancellationToken);

        if (string.IsNullOrWhiteSpace(auditAction))
        {
            return;
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = userId,
            Action = auditAction,
            TargetType = "User",
            TargetId = userId.ToString(),
            DataJson = JsonSerializer.Serialize(new
            {
                result.TokensRevoked,
                result.AuthorizationsRevoked,
                ip = httpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent = httpContext.Request.Headers.UserAgent.ToString(),
                traceId = httpContext.TraceIdentifier,
                metadata
            })
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
