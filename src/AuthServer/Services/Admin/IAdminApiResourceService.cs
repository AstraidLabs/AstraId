using AuthServer.Data;

namespace AuthServer.Services.Admin;

public interface IAdminApiResourceService
{
    Task<IReadOnlyList<ApiResource>> GetApiResourcesAsync(CancellationToken cancellationToken);
    Task<ApiResource?> GetApiResourceAsync(Guid apiResourceId, CancellationToken cancellationToken);
    Task<ApiResource?> GetApiResourceByNameAsync(string name, CancellationToken cancellationToken);
    Task<ApiResource> CreateApiResourceAsync(ApiResource apiResource, CancellationToken cancellationToken);
    Task UpdateApiResourceAsync(ApiResource apiResource, CancellationToken cancellationToken);
    Task<ApiResource> RotateApiKeyAsync(Guid apiResourceId, CancellationToken cancellationToken);
}
