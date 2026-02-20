using AuthServer.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Security;

/// <summary>
/// Provides login history service functionality.
/// </summary>
public sealed class LoginHistoryService
{
    private readonly ApplicationDbContext _dbContext;

    public LoginHistoryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordAsync(Guid? userId, string? enteredIdentifier, bool success, string? failureReasonCode, HttpContext httpContext, string? clientId, CancellationToken cancellationToken)
    {
        _dbContext.LoginHistory.Add(new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EnteredIdentifier = string.IsNullOrWhiteSpace(enteredIdentifier) ? null : enteredIdentifier,
            Success = success,
            FailureReasonCode = failureReasonCode,
            TimestampUtc = DateTime.UtcNow,
            Ip = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            ClientId = clientId,
            TraceId = httpContext.TraceIdentifier
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LoginHistory>> GetRecentForUserAsync(Guid userId, int take, CancellationToken cancellationToken)
    {
        var size = Math.Clamp(take, 1, 100);
        return await _dbContext.LoginHistory
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.TimestampUtc)
            .Take(size)
            .ToListAsync(cancellationToken);
    }
}
