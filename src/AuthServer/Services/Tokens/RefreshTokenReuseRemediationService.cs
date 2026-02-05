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
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ILogger<RefreshTokenReuseRemediationService> _logger;

    public RefreshTokenReuseRemediationService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOpenIddictApplicationManager applicationManager,
        ILogger<RefreshTokenReuseRemediationService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _applicationManager = applicationManager;
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
        string? applicationId = null;
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
            if (application is null)
            {
                _logger.LogWarning(
                    "Refresh token reuse detected for subject {Subject}, but client {ClientId} was not found.",
                    subject,
                    clientId);
            }
            else
            {
                applicationId = await _applicationManager.GetIdAsync(application, cancellationToken);
            }
        }

        var tokensQuery = _dbContext.Set<OpenIddictEntityFrameworkCoreToken>()
            .Where(token => token.Subject == subject);
        if (!string.IsNullOrWhiteSpace(applicationId))
        {
            tokensQuery = tokensQuery.Where(token => token.ApplicationId == applicationId);
        }

        var tokens = await tokensQuery.ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.Status = OpenIddictConstants.Statuses.Revoked;
            token.RedemptionDate = revokedAt;
        }

        var authorizationsQuery = _dbContext.Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .Where(authorization => authorization.Subject == subject);
        if (!string.IsNullOrWhiteSpace(applicationId))
        {
            authorizationsQuery = authorizationsQuery.Where(authorization => authorization.ApplicationId == applicationId);
        }

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
