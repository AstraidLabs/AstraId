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
    public DbSet<ClientState> ClientStates => Set<ClientState>();
    public DbSet<OidcResource> OidcResources => Set<OidcResource>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<SigningKeyRingEntry> SigningKeyRingEntries => Set<SigningKeyRingEntry>();
    public DbSet<TokenPolicyOverride> TokenPolicyOverrides => Set<TokenPolicyOverride>();
    public DbSet<ConsumedRefreshToken> ConsumedRefreshTokens => Set<ConsumedRefreshToken>();

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

        builder.Entity<ClientState>(entity =>
        {
            entity.HasKey(clientState => clientState.ApplicationId);
            entity.Property(clientState => clientState.ApplicationId).HasMaxLength(200);
            entity.Property(clientState => clientState.UpdatedUtc);
        });

        builder.Entity<OidcResource>(entity =>
        {
            entity.HasKey(resource => resource.Id);
            entity.HasIndex(resource => resource.Name).IsUnique();
            entity.Property(resource => resource.Name).HasMaxLength(200);
            entity.Property(resource => resource.DisplayName).HasMaxLength(200);
            entity.Property(resource => resource.Description).HasMaxLength(500);
            entity.Property(resource => resource.IsActive);
            entity.Property(resource => resource.CreatedUtc);
            entity.Property(resource => resource.UpdatedUtc);
        });

        builder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(error => error.Id);
            entity.HasIndex(error => error.TimestampUtc);
            entity.HasIndex(error => error.TraceId);
            entity.Property(error => error.TraceId).HasMaxLength(128);
            entity.Property(error => error.Path).HasMaxLength(2048);
            entity.Property(error => error.Method).HasMaxLength(32);
            entity.Property(error => error.Title).HasMaxLength(200);
            entity.Property(error => error.Detail).HasMaxLength(2000);
            entity.Property(error => error.ExceptionType).HasMaxLength(500);
            entity.Property(error => error.InnerException).HasMaxLength(2000);
            entity.Property(error => error.UserAgent).HasMaxLength(500);
            entity.Property(error => error.RemoteIp).HasMaxLength(64);
        });

        builder.Entity<SigningKeyRingEntry>(entity =>
        {
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => entry.Kid).IsUnique();
            entity.HasIndex(entry => entry.Status);
            entity.Property(entry => entry.Kid).HasMaxLength(200);
            entity.Property(entry => entry.Algorithm).HasMaxLength(20);
            entity.Property(entry => entry.KeyType).HasMaxLength(20);
            entity.Property(entry => entry.PublicJwkJson);
            entity.Property(entry => entry.PrivateKeyProtected);
            entity.Property(entry => entry.MetadataJson);
        });

        builder.Entity<TokenPolicyOverride>(entity =>
        {
            entity.HasKey(policy => policy.Id);
            entity.HasIndex(policy => policy.UpdatedUtc);
        });

        builder.Entity<ConsumedRefreshToken>(entity =>
        {
            entity.HasKey(token => token.TokenId);
            entity.Property(token => token.TokenId).HasMaxLength(200);
            entity.HasIndex(token => token.ExpiresUtc);
        });
    }
}
