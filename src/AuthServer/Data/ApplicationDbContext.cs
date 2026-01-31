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
    public DbSet<ApiResource> ApiResources => Set<ApiResource>();
    public DbSet<ApiEndpoint> ApiEndpoints => Set<ApiEndpoint>();
    public DbSet<EndpointPermission> EndpointPermissions => Set<EndpointPermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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

        builder.Entity<ApiResource>(entity =>
        {
            entity.HasKey(apiResource => apiResource.Id);
            entity.HasIndex(apiResource => apiResource.Name).IsUnique();
            entity.Property(apiResource => apiResource.Name).HasMaxLength(200);
            entity.Property(apiResource => apiResource.DisplayName).HasMaxLength(200);
        });

        builder.Entity<ApiEndpoint>(entity =>
        {
            entity.HasKey(apiEndpoint => apiEndpoint.Id);
            entity.HasIndex(apiEndpoint => new { apiEndpoint.ApiResourceId, apiEndpoint.Method, apiEndpoint.Path })
                .IsUnique();
            entity.Property(apiEndpoint => apiEndpoint.Method).HasMaxLength(16);
            entity.Property(apiEndpoint => apiEndpoint.Path).HasMaxLength(500);
            entity.Property(apiEndpoint => apiEndpoint.DisplayName).HasMaxLength(200);
            entity.Property(apiEndpoint => apiEndpoint.Tags).HasMaxLength(500);
            entity.HasOne(apiEndpoint => apiEndpoint.ApiResource)
                .WithMany(apiResource => apiResource.Endpoints)
                .HasForeignKey(apiEndpoint => apiEndpoint.ApiResourceId);
        });

        builder.Entity<EndpointPermission>(entity =>
        {
            entity.HasKey(endpointPermission => new { endpointPermission.EndpointId, endpointPermission.PermissionId });
            entity.HasOne(endpointPermission => endpointPermission.Endpoint)
                .WithMany(endpoint => endpoint.EndpointPermissions)
                .HasForeignKey(endpointPermission => endpointPermission.EndpointId);
            entity.HasOne(endpointPermission => endpointPermission.Permission)
                .WithMany()
                .HasForeignKey(endpointPermission => endpointPermission.PermissionId);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(auditLog => auditLog.Id);
            entity.HasIndex(auditLog => auditLog.TimestampUtc);
            entity.Property(auditLog => auditLog.Action).HasMaxLength(200);
            entity.Property(auditLog => auditLog.TargetType).HasMaxLength(200);
        });
    }
}
