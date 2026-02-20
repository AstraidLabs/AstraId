using AuthServer.Data;

namespace AuthServer.Services.Admin;

/// <summary>
/// Defines the contract for admin api resource service.
/// </summary>
public interface IAdminApiResourceService
{
    Task<IReadOnlyList<ApiResource>> GetApiResourcesAsync(CancellationToken cancellationToken);
    Task<ApiResource?> GetApiResourceAsync(Guid apiResourceId, CancellationToken cancellationToken);
    Task<ApiResource?> GetApiResourceByNameAsync(string name, CancellationToken cancellationToken);
    Task<ApiResource> CreateApiResourceAsync(ApiResource apiResource, CancellationToken cancellationToken);
    Task UpdateApiResourceAsync(ApiResource apiResource, CancellationToken cancellationToken);
    Task<ApiResource> RotateApiKeyAsync(Guid apiResourceId, CancellationToken cancellationToken);
}
