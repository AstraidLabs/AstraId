namespace AuthServer.Services.Admin.Models;

public sealed record AdminApiEndpointListItem(
    Guid Id,
    string Method,
    string Path,
    string? DisplayName,
    bool IsDeprecated,
    bool IsActive,
    IReadOnlyList<Guid> PermissionIds,
    IReadOnlyList<string> PermissionKeys);
