namespace AuthServer.Services.Admin.Models;

public sealed record AdminRoleDetail(
    Guid Id,
    string Name,
    bool IsSystem,
    IReadOnlyList<Guid> PermissionIds);
