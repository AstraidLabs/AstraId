namespace AuthServer.Services.Admin.Models;

public sealed record AdminPermissionRequest(
    string Key,
    string Description,
    string Group,
    bool IsSystem);
