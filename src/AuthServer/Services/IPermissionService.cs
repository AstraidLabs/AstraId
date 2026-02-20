namespace AuthServer.Services;

/// <summary>
/// Defines the contract for permission service.
/// </summary>
public interface IPermissionService
{
    Task<string[]> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<string[]> GetPermissionsForRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);
}
