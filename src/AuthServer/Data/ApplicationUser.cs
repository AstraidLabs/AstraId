using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
    public bool IsAnonymized { get; set; }
    public DateTime? DeactivatedUtc { get; set; }
    public DateTime? AnonymizedUtc { get; set; }
    public DateTime? RequestedDeletionUtc { get; set; }
}
