namespace AuthServer.Services.Admin.Models;

public sealed record AdminUserRolesRequest(IReadOnlyList<string> Roles);
