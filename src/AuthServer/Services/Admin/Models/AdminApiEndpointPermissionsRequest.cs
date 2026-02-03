namespace AuthServer.Services.Admin.Models;

public sealed record AdminApiEndpointPermissionsRequest(IReadOnlyList<Guid> PermissionIds);
