using System;
using AuthServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using OpenIddict.EntityFrameworkCore.Models;

#nullable disable

namespace AuthServer.Migrations;

[DbContext(typeof(ApplicationDbContext))]
partial class ApplicationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.0-preview.7")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("AuthServer.Data.Permission", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Description")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("Group")
                .IsRequired()
                .HasColumnType("text");

            b.Property<bool>("IsSystem")
                .HasColumnType("boolean");

            b.Property<string>("Key")
                .IsRequired()
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("Key")
                .IsUnique();

            b.ToTable("Permissions");
        });

        modelBuilder.Entity("AuthServer.Data.RolePermission", b =>
        {
            b.Property<Guid>("RoleId")
                .HasColumnType("uuid");

            b.Property<Guid>("PermissionId")
                .HasColumnType("uuid");

            b.HasKey("RoleId", "PermissionId");

            b.HasIndex("PermissionId");

            b.ToTable("RolePermissions");
        });

        modelBuilder.Entity("AuthServer.Data.ApplicationUser", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<int>("AccessFailedCount")
                .HasColumnType("integer");

            b.Property<string>("ConcurrencyStamp")
                .IsConcurrencyToken()
                .HasColumnType("text");

            b.Property<string>("Email")
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            b.Property<bool>("EmailConfirmed")
                .HasColumnType("boolean");

            b.Property<bool>("LockoutEnabled")
                .HasColumnType("boolean");

            b.Property<DateTimeOffset?>("LockoutEnd")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("NormalizedEmail")
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            b.Property<string>("NormalizedUserName")
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            b.Property<string>("PasswordHash")
                .HasColumnType("text");

            b.Property<string>("PhoneNumber")
                .HasColumnType("text");

            b.Property<bool>("PhoneNumberConfirmed")
                .HasColumnType("boolean");

            b.Property<string>("SecurityStamp")
                .HasColumnType("text");

            b.Property<bool>("TwoFactorEnabled")
                .HasColumnType("boolean");

            b.Property<string>("UserName")
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            b.HasKey("Id");

            b.HasIndex("NormalizedEmail")
                .HasDatabaseName("EmailIndex");

            b.HasIndex("NormalizedUserName")
                .IsUnique()
                .HasDatabaseName("UserNameIndex");

            b.ToTable("AspNetUsers", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("ConcurrencyStamp")
                .IsConcurrencyToken()
                .HasColumnType("text");

            b.Property<string>("Name")
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            b.Property<string>("NormalizedName")
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            b.HasKey("Id");

            b.HasIndex("NormalizedName")
                .IsUnique()
                .HasDatabaseName("RoleNameIndex");

            b.ToTable("AspNetRoles", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer");

            b.Property<string>("ClaimType")
                .HasColumnType("text");

            b.Property<string>("ClaimValue")
                .HasColumnType("text");

            b.Property<Guid>("RoleId")
                .HasColumnType("uuid");

            b.HasKey("Id");

            b.HasIndex("RoleId");

            b.ToTable("AspNetRoleClaims", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer");

            b.Property<string>("ClaimType")
                .HasColumnType("text");

            b.Property<string>("ClaimValue")
                .HasColumnType("text");

            b.Property<Guid>("UserId")
                .HasColumnType("uuid");

            b.HasKey("Id");

            b.HasIndex("UserId");

            b.ToTable("AspNetUserClaims", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
        {
            b.Property<string>("LoginProvider")
                .HasColumnType("text");

            b.Property<string>("ProviderKey")
                .HasColumnType("text");

            b.Property<string>("ProviderDisplayName")
                .HasColumnType("text");

            b.Property<Guid>("UserId")
                .HasColumnType("uuid");

            b.HasKey("LoginProvider", "ProviderKey");

            b.HasIndex("UserId");

            b.ToTable("AspNetUserLogins", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
        {
            b.Property<Guid>("UserId")
                .HasColumnType("uuid");

            b.Property<Guid>("RoleId")
                .HasColumnType("uuid");

            b.HasKey("UserId", "RoleId");

            b.HasIndex("RoleId");

            b.ToTable("AspNetUserRoles", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
        {
            b.Property<Guid>("UserId")
                .HasColumnType("uuid");

            b.Property<string>("LoginProvider")
                .HasColumnType("text");

            b.Property<string>("Name")
                .HasColumnType("text");

            b.Property<string>("Value")
                .HasColumnType("text");

            b.HasKey("UserId", "LoginProvider", "Name");

            b.ToTable("AspNetUserTokens", (string)null);
        });

        modelBuilder.Entity("OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreApplication", b =>
        {
            b.Property<string>("Id")
                .HasColumnType("text");

            b.Property<string>("ClientId")
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<string>("ClientSecret")
                .HasColumnType("text");

            b.Property<string>("ConcurrencyToken")
                .IsConcurrencyToken()
                .HasColumnType("text");

            b.Property<string>("ConsentType")
                .HasColumnType("text");

            b.Property<string>("DisplayName")
                .HasColumnType("text");

            b.Property<string>("DisplayNames")
                .HasColumnType("text");

            b.Property<string>("Permissions")
                .HasColumnType("text");

            b.Property<string>("PostLogoutRedirectUris")
                .HasColumnType("text");

            b.Property<string>("Properties")
                .HasColumnType("text");

            b.Property<string>("RedirectUris")
                .HasColumnType("text");

            b.Property<string>("Requirements")
                .HasColumnType("text");

            b.Property<string>("Type")
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("ClientId")
                .IsUnique();

            b.ToTable("OpenIddictApplications", (string)null);
        });

        modelBuilder.Entity("OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreAuthorization", b =>
        {
            b.Property<string>("Id")
                .HasColumnType("text");

            b.Property<string>("ApplicationId")
                .HasColumnType("text");

            b.Property<string>("ConcurrencyToken")
                .IsConcurrencyToken()
                .HasColumnType("text");

            b.Property<DateTimeOffset?>("CreationDate")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Properties")
                .HasColumnType("text");

            b.Property<string>("Scopes")
                .HasColumnType("text");

            b.Property<string>("Status")
                .HasColumnType("text");

            b.Property<string>("Subject")
                .HasColumnType("text");

            b.Property<string>("Type")
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("ApplicationId");

            b.ToTable("OpenIddictAuthorizations", (string)null);
        });

        modelBuilder.Entity("OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreScope", b =>
        {
            b.Property<string>("Id")
                .HasColumnType("text");

            b.Property<string>("ConcurrencyToken")
                .IsConcurrencyToken()
                .HasColumnType("text");

            b.Property<string>("Description")
                .HasColumnType("text");

            b.Property<string>("Descriptions")
                .HasColumnType("text");

            b.Property<string>("DisplayName")
                .HasColumnType("text");

            b.Property<string>("DisplayNames")
                .HasColumnType("text");

            b.Property<string>("Name")
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<string>("Properties")
                .HasColumnType("text");

            b.Property<string>("Resources")
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("Name")
                .IsUnique();

            b.ToTable("OpenIddictScopes", (string)null);
        });

        modelBuilder.Entity("OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreToken", b =>
        {
            b.Property<string>("Id")
                .HasColumnType("text");

            b.Property<string>("ApplicationId")
                .HasColumnType("text");

            b.Property<string>("AuthorizationId")
                .HasColumnType("text");

            b.Property<string>("ConcurrencyToken")
                .IsConcurrencyToken()
                .HasColumnType("text");

            b.Property<DateTimeOffset?>("CreationDate")
                .HasColumnType("timestamp with time zone");

            b.Property<DateTimeOffset?>("ExpirationDate")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Payload")
                .HasColumnType("text");

            b.Property<string>("Properties")
                .HasColumnType("text");

            b.Property<DateTimeOffset?>("RedemptionDate")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("ReferenceId")
                .HasColumnType("text");

            b.Property<string>("Status")
                .HasColumnType("text");

            b.Property<string>("Subject")
                .HasColumnType("text");

            b.Property<string>("Type")
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("ApplicationId");

            b.HasIndex("AuthorizationId");

            b.HasIndex("ReferenceId")
                .IsUnique();

            b.ToTable("OpenIddictTokens", (string)null);
        });

        modelBuilder.Entity("AuthServer.Data.RolePermission", b =>
        {
            b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", "Role")
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("AuthServer.Data.Permission", "Permission")
                .WithMany("RolePermissions")
                .HasForeignKey("PermissionId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Permission");

            b.Navigation("Role");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
        {
            b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
        {
            b.HasOne("AuthServer.Data.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
        {
            b.HasOne("AuthServer.Data.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
        {
            b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("AuthServer.Data.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
        {
            b.HasOne("AuthServer.Data.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreAuthorization", b =>
        {
            b.HasOne("OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreApplication", null)
                .WithMany()
                .HasForeignKey("ApplicationId");
        });

        modelBuilder.Entity("OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreToken", b =>
        {
            b.HasOne("OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreApplication", null)
                .WithMany()
                .HasForeignKey("ApplicationId");

            b.HasOne("OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreAuthorization", null)
                .WithMany()
                .HasForeignKey("AuthorizationId");
        });
    }
}
