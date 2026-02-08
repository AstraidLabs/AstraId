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
    public DbSet<KeyRotationPolicy> KeyRotationPolicies => Set<KeyRotationPolicy>();
    public DbSet<TokenPolicy> TokenPolicies => Set<TokenPolicy>();
    public DbSet<TokenIncident> TokenIncidents => Set<TokenIncident>();
    public DbSet<TokenPolicyOverride> TokenPolicyOverrides => Set<TokenPolicyOverride>();
    public DbSet<ConsumedRefreshToken> ConsumedRefreshTokens => Set<ConsumedRefreshToken>();
    public DbSet<UserSecurityEvent> UserSecurityEvents => Set<UserSecurityEvent>();
    public DbSet<UserLifecyclePolicy> UserLifecyclePolicies => Set<UserLifecyclePolicy>();
    public DbSet<UserActivity> UserActivities => Set<UserActivity>();
    public DbSet<DeletionRequest> DeletionRequests => Set<DeletionRequest>();
    public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();
    public DbSet<PrivacyPolicy> PrivacyPolicies => Set<PrivacyPolicy>();
    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();
    public DbSet<InactivityPolicy> InactivityPolicies => Set<InactivityPolicy>();

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
            entity.Property(clientState => clientState.Profile).HasMaxLength(100);
            entity.Property(clientState => clientState.AppliedPresetId).HasMaxLength(100);
            entity.Property(clientState => clientState.OverridesJson);
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
            entity.Property(entry => entry.PrivateMaterialProtected).HasColumnName("PrivateKeyProtected");
            entity.Property(entry => entry.Thumbprint).HasMaxLength(128);
            entity.Property(entry => entry.Comment).HasMaxLength(500);
            entity.Property(entry => entry.CreatedByUserId);
            entity.Property(entry => entry.MetadataJson);
            entity.Property(entry => entry.RetireAfterUtc);
            entity.Property(entry => entry.RevokedUtc);
        });

        builder.Entity<KeyRotationPolicy>(entity =>
        {
            entity.HasKey(policy => policy.Id);
            entity.HasIndex(policy => policy.UpdatedUtc);
        });

        builder.Entity<TokenPolicy>(entity =>
        {
            entity.HasKey(policy => policy.Id);
            entity.HasIndex(policy => policy.UpdatedUtc);
        });

        builder.Entity<TokenIncident>(entity =>
        {
            entity.HasKey(incident => incident.Id);
            entity.HasIndex(incident => incident.TimestampUtc);
            entity.HasIndex(incident => incident.Type);
            entity.HasIndex(incident => incident.Severity);
            entity.Property(incident => incident.Type).HasMaxLength(200);
            entity.Property(incident => incident.Severity).HasMaxLength(50);
            entity.Property(incident => incident.ClientId).HasMaxLength(200);
            entity.Property(incident => incident.TraceId).HasMaxLength(128);
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

        builder.Entity<UserSecurityEvent>(entity =>
        {
            entity.HasKey(evt => evt.Id);
            entity.HasIndex(evt => new { evt.UserId, evt.TimestampUtc });
            entity.Property(evt => evt.EventType).HasMaxLength(120);
            entity.Property(evt => evt.IpAddress).HasMaxLength(64);
            entity.Property(evt => evt.UserAgent).HasMaxLength(1024);
            entity.Property(evt => evt.ClientId).HasMaxLength(200);
            entity.Property(evt => evt.TraceId).HasMaxLength(128);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(evt => evt.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AuditLog>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(log => log.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ErrorLog>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(log => log.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<TokenIncident>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(incident => incident.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<TokenIncident>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(incident => incident.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<UserLifecyclePolicy>(entity =>
        {
            entity.HasKey(policy => policy.Id);
            entity.Property(policy => policy.UpdatedUtc);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(policy => policy.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<UserActivity>(entity =>
        {
            entity.HasKey(activity => activity.UserId);
            entity.HasIndex(activity => activity.LastSeenUtc);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(activity => activity.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DeletionRequest>(entity =>
        {
            entity.HasKey(request => request.Id);
            entity.HasIndex(request => new { request.UserId, request.RequestedUtc });
            entity.Property(request => request.Reason).HasMaxLength(1000);
            entity.HasOne(request => request.User)
                .WithMany()
                .HasForeignKey(request => request.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(request => request.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<LoginHistory>(entity =>
        {
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => entry.TimestampUtc);
            entity.HasIndex(entry => new { entry.UserId, entry.TimestampUtc });
            entity.Property(entry => entry.EnteredIdentifier).HasMaxLength(256);
            entity.Property(entry => entry.FailureReasonCode).HasMaxLength(128);
            entity.Property(entry => entry.Ip).HasMaxLength(64);
            entity.Property(entry => entry.UserAgent).HasMaxLength(1024);
            entity.Property(entry => entry.ClientId).HasMaxLength(200);
            entity.Property(entry => entry.TraceId).HasMaxLength(128);
            entity.HasOne(entry => entry.User)
                .WithMany()
                .HasForeignKey(entry => entry.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PrivacyPolicy>(entity =>
        {
            entity.HasKey(policy => policy.Id);
            entity.HasIndex(policy => policy.UpdatedUtc);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(policy => policy.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<EmailOutboxMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.HasIndex(message => new { message.Status, message.NextAttemptUtc });
            entity.HasIndex(message => new { message.UserId, message.CreatedUtc });
            entity.HasIndex(message => message.IdempotencyKey).IsUnique().HasFilter(""IdempotencyKey" IS NOT NULL");
            entity.Property(message => message.Type).HasMaxLength(120);
            entity.Property(message => message.Subject).HasMaxLength(300);
            entity.Property(message => message.ToEmail).HasMaxLength(320);
            entity.Property(message => message.ToName).HasMaxLength(200);
            entity.Property(message => message.Error).HasMaxLength(4000);
            entity.Property(message => message.TraceId).HasMaxLength(128);
            entity.Property(message => message.CorrelationId).HasMaxLength(128);
            entity.Property(message => message.IdempotencyKey).HasMaxLength(200);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(message => message.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<InactivityPolicy>(entity =>
        {
            entity.HasKey(policy => policy.Id);
            entity.HasIndex(policy => policy.UpdatedUtc);
            entity.Property(policy => policy.ProtectedRoles).HasMaxLength(500);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(policy => policy.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
