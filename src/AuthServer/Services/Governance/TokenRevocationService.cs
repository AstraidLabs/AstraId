using AuthServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;

namespace AuthServer.Services.Governance;

public sealed record TokenRevocationResult(int TokensRevoked, int AuthorizationsRevoked);

public sealed class TokenRevocationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOpenIddictApplicationManager _applicationManager;

    public TokenRevocationService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOpenIddictApplicationManager applicationManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _applicationManager = applicationManager;
    }

    public async Task<TokenRevocationResult> RevokeUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await RevokeAsync(userId.ToString(), clientId: null, cancellationToken);
    }

    public async Task<TokenRevocationResult> RevokeClientAsync(string clientId, CancellationToken cancellationToken)
    {
        return await RevokeAsync(subject: null, clientId, cancellationToken);
    }

    public async Task<TokenRevocationResult> RevokeUserClientAsync(Guid userId, string clientId, CancellationToken cancellationToken)
    {
        return await RevokeAsync(userId.ToString(), clientId, cancellationToken);
    }

    private async Task<TokenRevocationResult> RevokeAsync(string? subject, string? clientId, CancellationToken cancellationToken)
    {
        string? applicationId = null;
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
            if (application is not null)
            {
                applicationId = await _applicationManager.GetIdAsync(application, cancellationToken);
            }
        }

        var tokensQuery = _dbContext.Set<OpenIddictEntityFrameworkCoreToken>().AsQueryable();
        var authQuery = _dbContext.Set<OpenIddictEntityFrameworkCoreAuthorization>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(subject))
        {
            tokensQuery = tokensQuery.Where(token => token.Subject == subject);
            authQuery = authQuery.Where(auth => auth.Subject == subject);
        }

        if (!string.IsNullOrWhiteSpace(applicationId))
        {
            tokensQuery = tokensQuery.Where(token => token.ApplicationId == applicationId);
            authQuery = authQuery.Where(auth => auth.ApplicationId == applicationId);
        }

        var tokens = await tokensQuery.ToListAsync(cancellationToken);
        var revokedAt = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.Status = OpenIddictConstants.Statuses.Revoked;
            token.RedemptionDate = revokedAt;
        }

        var authorizations = await authQuery.ToListAsync(cancellationToken);
        foreach (var authorization in authorizations)
        {
            authorization.Status = OpenIddictConstants.Statuses.Revoked;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(subject) && Guid.TryParse(subject, out var userId))
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is not null)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }
        }

        return new TokenRevocationResult(tokens.Count, authorizations.Count);
    }
}
