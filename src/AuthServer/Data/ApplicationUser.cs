using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
}
