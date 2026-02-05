using System.Security.Claims;
using AuthServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;

namespace AuthServer.Services.Tokens;

public sealed class RefreshTokenReuseRemediationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RefreshTokenReuseRemediationService> _logger;

    public RefreshTokenReuseRemediationService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<RefreshTokenReuseRemediationService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task RevokeSubjectTokensAsync(
        ClaimsPrincipal principal,
        string? clientId,
        CancellationToken cancellationToken)
    {
        var subject = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrWhiteSpace(subject))
        {
            _logger.LogWarning("Refresh token reuse detected without subject claim.");
            return;
        }

        var revokedAt = DateTime.UtcNow;
        var tokensQuery = _dbContext.Set<OpenIddictEntityFrameworkCoreToken>()
            .Where(token => token.Subject == subject);

        var tokens = await tokensQuery.ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.Status = OpenIddictConstants.Statuses.Revoked;
            token.RedemptionDate = revokedAt;
        }

        var authorizationsQuery = _dbContext.Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .Where(authorization => authorization.Subject == subject);

        var authorizations = await authorizationsQuery.ToListAsync(cancellationToken);
        foreach (var authorization in authorizations)
        {
            authorization.Status = OpenIddictConstants.Statuses.Revoked;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (Guid.TryParse(subject, out var userId))
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is not null)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }
        }
    }
}
