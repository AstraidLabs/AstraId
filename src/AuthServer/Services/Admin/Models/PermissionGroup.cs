using AuthServer.Data;

namespace AuthServer.Services.Admin.Models;

public sealed record PermissionGroup(string Group, IReadOnlyList<Permission> Permissions);
