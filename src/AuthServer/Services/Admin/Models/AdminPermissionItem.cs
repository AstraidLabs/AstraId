namespace AuthServer.Services.Admin.Models;

public sealed record AdminPermissionItem(
    Guid Id,
    string Key,
    string Description,
    string Group,
    bool IsSystem);
