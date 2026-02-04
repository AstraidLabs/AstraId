using AuthServer.Services.Admin.Models;

namespace AuthServer.Services.Admin;

public interface IAdminOidcScopeService
{
    Task<PagedResult<AdminOidcScopeListItem>> GetScopesAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<AdminOidcScopeDetail?> GetScopeAsync(string nameOrId, CancellationToken cancellationToken);
    Task<AdminOidcScopeUsage?> GetScopeUsageAsync(string nameOrId, CancellationToken cancellationToken);

    Task<AdminOidcScopeDetail> CreateScopeAsync(AdminOidcScopeRequest request, CancellationToken cancellationToken);

    Task<AdminOidcScopeDetail?> UpdateScopeAsync(string nameOrId, AdminOidcScopeRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteScopeAsync(string nameOrId, CancellationToken cancellationToken);
}
