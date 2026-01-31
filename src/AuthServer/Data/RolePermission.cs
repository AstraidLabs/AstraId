using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data;

public class RolePermission
{
    public Guid RoleId { get; set; }
    public IdentityRole<Guid>? Role { get; set; }

    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
}
