namespace AuthServer.Services;

public interface IPermissionService
{
    Task<string[]> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<string[]> GetPermissionsForRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);
}
