using AuthServer.Data;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace AuthServer.Services.Tokens;

public sealed class RefreshTokenReuseDetectionService
{
    private readonly ApplicationDbContext _dbContext;

    public RefreshTokenReuseDetectionService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshTokenReuseResult> TryConsumeAsync(
        ClaimsPrincipal principal,
        int leewaySeconds,
        CancellationToken cancellationToken)
    {
        var tokenId = principal.GetClaim(OpenIddictConstants.Claims.Private.TokenId);
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return RefreshTokenReuseResult.MissingTokenId;
        }

        var now = DateTime.UtcNow;
        var existing = await _dbContext.ConsumedRefreshTokens
            .FirstOrDefaultAsync(entry => entry.TokenId == tokenId, cancellationToken);

        if (existing is not null)
        {
            var leeway = TimeSpan.FromSeconds(Math.Max(0, leewaySeconds));
            if (existing.ConsumedUtc.Add(leeway) >= now)
            {
                return RefreshTokenReuseResult.WithinLeeway;
            }

            return RefreshTokenReuseResult.Reused;
        }

        var expiresUtc = principal.GetExpirationDate()?.UtcDateTime;
        _dbContext.ConsumedRefreshTokens.Add(new ConsumedRefreshToken
        {
            TokenId = tokenId,
            ConsumedUtc = now,
            ExpiresUtc = expiresUtc
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RefreshTokenReuseResult.Consumed;
    }
}

public enum RefreshTokenReuseResult
{
    Consumed = 0,
    WithinLeeway = 1,
    Reused = 2,
    MissingTokenId = 3
}
