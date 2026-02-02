using AuthServer.Data;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthServer.Services;

public interface IClientStateService
{
    Task<bool> IsClientEnabledAsync(string? clientId, CancellationToken cancellationToken);
}

public sealed class ClientStateService : IClientStateService
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ApplicationDbContext _dbContext;

    public ClientStateService(
        IOpenIddictApplicationManager applicationManager,
        ApplicationDbContext dbContext)
    {
        _applicationManager = applicationManager;
        _dbContext = dbContext;
    }

    public async Task<bool> IsClientEnabledAsync(string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return true;
        }

        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            return true;
        }

        var applicationId = await _applicationManager.GetIdAsync(application, cancellationToken);
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return true;
        }

        var state = await _dbContext.ClientStates
            .AsNoTracking()
            .FirstOrDefaultAsync(clientState => clientState.ApplicationId == applicationId, cancellationToken);

        return state?.Enabled ?? true;
    }
}
