namespace AuthServer.Services.Admin.Models;

public sealed record AdminRolePermissionsRequest(IReadOnlyList<Guid> PermissionIds);
