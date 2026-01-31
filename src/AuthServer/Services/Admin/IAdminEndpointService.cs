using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services.Admin.Models;

namespace AuthServer.Services.Admin;

public interface IAdminEndpointService
{
    Task<IReadOnlyList<ApiEndpoint>> GetEndpointsAsync(Guid apiResourceId, CancellationToken cancellationToken);
    Task<ApiEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken cancellationToken);
    Task<ApiEndpoint> CreateEndpointAsync(ApiEndpoint endpoint, CancellationToken cancellationToken);
    Task UpdateEndpointAsync(ApiEndpoint endpoint, CancellationToken cancellationToken);
    Task SetEndpointPermissionsAsync(Guid endpointId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetEndpointPermissionIdsAsync(Guid endpointId, CancellationToken cancellationToken);
    Task<EndpointSyncResult> SyncEndpointsAsync(ApiResource apiResource, IReadOnlyCollection<ApiEndpointSyncDto> endpoints, CancellationToken cancellationToken);
}
