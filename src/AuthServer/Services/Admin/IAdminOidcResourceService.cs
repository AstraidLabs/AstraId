using AuthServer.Services.Admin.Models;

namespace AuthServer.Services.Admin;

public interface IAdminOidcResourceService
{
    Task<PagedResult<AdminOidcResourceListItem>> GetResourcesAsync(
        string? search,
        int page,
        int pageSize,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<AdminOidcResourceDetail?> GetResourceAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminOidcResourceUsage?> GetResourceUsageAsync(Guid id, CancellationToken cancellationToken);

    Task<AdminOidcResourceDetail> CreateResourceAsync(AdminOidcResourceRequest request, CancellationToken cancellationToken);

    Task<AdminOidcResourceDetail?> UpdateResourceAsync(Guid id, AdminOidcResourceRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteResourceAsync(Guid id, CancellationToken cancellationToken);
}
