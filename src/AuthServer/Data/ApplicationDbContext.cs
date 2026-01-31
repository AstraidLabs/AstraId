using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Permission>(entity =>
        {
            entity.HasKey(permission => permission.Id);
            entity.HasIndex(permission => permission.Key).IsUnique();
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });
            entity.HasOne(rolePermission => rolePermission.Role)
                .WithMany()
                .HasForeignKey(rolePermission => rolePermission.RoleId);
            entity.HasOne(rolePermission => rolePermission.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(rolePermission => rolePermission.PermissionId);
        });
    }
}
