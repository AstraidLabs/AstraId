using AuthServer.Data;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthServer.Seeding;

public sealed class AuthBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthBootstrapHostedService> _logger;

    public AuthBootstrapHostedService(
        IServiceProvider serviceProvider,
        IWebHostEnvironment environment,
        ILogger<AuthBootstrapHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        await SyncPermissionsAsync(dbContext, cancellationToken);

        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await SyncScopesAsync(scopeManager, cancellationToken);
        await SyncApplicationsAsync(applicationManager, cancellationToken);

        await SeedAdminAsync(scope.ServiceProvider, dbContext, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SyncPermissionsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingPermissions = await dbContext.Permissions
            .ToListAsync(cancellationToken);

        foreach (var definition in AuthServerDefinitions.Permissions)
        {
            var permission = existingPermissions.FirstOrDefault(item => item.Key == definition.Key);
            if (permission is null)
            {
                dbContext.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Key = definition.Key,
                    Description = definition.Description,
                    Group = definition.Group,
                    IsSystem = true
                });
                continue;
            }

            permission.Description = definition.Description;
            permission.Group = definition.Group;
            permission.IsSystem = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SyncScopesAsync(IOpenIddictScopeManager scopeManager, CancellationToken cancellationToken)
    {
        foreach (var apiResource in AuthServerDefinitions.ApiResources)
        {
            foreach (var scopeName in apiResource.Scopes)
            {
                var scope = await scopeManager.FindByNameAsync(scopeName, cancellationToken);
                var displayName = scopeName.Equals(apiResource.Name, StringComparison.Ordinal)
                    ? apiResource.DisplayName
                    : $"{apiResource.DisplayName} ({scopeName})";

                if (scope is null)
                {
                    var newScope = new OpenIddictScopeDescriptor
                    {
                        Name = scopeName,
                        DisplayName = displayName
                    };

                    newScope.Resources.Add(apiResource.Resource);

                    await scopeManager.CreateAsync(newScope, cancellationToken);
                    continue;
                }

                var descriptor = new OpenIddictScopeDescriptor();
                await scopeManager.PopulateAsync(descriptor, scope, cancellationToken);
                descriptor.DisplayName = displayName;
                descriptor.Resources.Clear();
                descriptor.Resources.Add(apiResource.Resource);
                await scopeManager.UpdateAsync(scope, descriptor, cancellationToken);
            }
        }
    }

    private async Task SyncApplicationsAsync(IOpenIddictApplicationManager applicationManager, CancellationToken cancellationToken)
    {
        foreach (var client in AuthServerDefinitions.Clients)
        {
            var application = await applicationManager.FindByClientIdAsync(client.ClientId, cancellationToken);
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = client.ClientId,
                DisplayName = client.DisplayName,
                Type = client.Type
            };

            foreach (var uri in client.RedirectUris)
            {
                descriptor.RedirectUris.Add(uri);
            }

            foreach (var uri in client.PostLogoutRedirectUris)
            {
                descriptor.PostLogoutRedirectUris.Add(uri);
            }

            foreach (var permission in BuildPermissions(client))
            {
                descriptor.Permissions.Add(permission);
            }

            foreach (var requirement in BuildRequirements(client))
            {
                descriptor.Requirements.Add(requirement);
            }

            if (application is null)
            {
                await applicationManager.CreateAsync(descriptor, cancellationToken);
                continue;
            }

            var currentDescriptor = new OpenIddictApplicationDescriptor();
            await applicationManager.PopulateAsync(currentDescriptor, application, cancellationToken);

            currentDescriptor.DisplayName = descriptor.DisplayName;
            currentDescriptor.Type = descriptor.Type;

            currentDescriptor.RedirectUris.Clear();
            foreach (var uri in descriptor.RedirectUris)
            {
                currentDescriptor.RedirectUris.Add(uri);
            }

            currentDescriptor.PostLogoutRedirectUris.Clear();
            foreach (var uri in descriptor.PostLogoutRedirectUris)
            {
                currentDescriptor.PostLogoutRedirectUris.Add(uri);
            }

            currentDescriptor.Permissions.Clear();
            foreach (var permission in descriptor.Permissions)
            {
                currentDescriptor.Permissions.Add(permission);
            }

            currentDescriptor.Requirements.Clear();
            foreach (var requirement in descriptor.Requirements)
            {
                currentDescriptor.Requirements.Add(requirement);
            }

            await applicationManager.UpdateAsync(application, currentDescriptor, cancellationToken);
        }
    }

    private static IEnumerable<string> BuildPermissions(OAuthClientDefinition client)
    {
        var permissions = new HashSet<string>
        {
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.Endpoints.Logout,
            OpenIddictConstants.Permissions.Endpoints.Userinfo
        };

        foreach (var grantType in client.AllowedGrantTypes)
        {
            switch (grantType)
            {
                case OpenIddictConstants.GrantTypes.AuthorizationCode:
                    permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
                    permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
                    break;
                case OpenIddictConstants.GrantTypes.RefreshToken:
                    permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
                    break;
            }
        }

        foreach (var scope in client.Scopes)
        {
            permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        return permissions;
    }

    private static IEnumerable<string> BuildRequirements(OAuthClientDefinition client)
    {
        if (string.Equals(client.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.Ordinal))
        {
            return [OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange];
        }

        return Array.Empty<string>();
    }

    private async Task SeedAdminAsync(
        IServiceProvider serviceProvider,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var adminRole = await roleManager.FindByNameAsync("Admin");
        if (adminRole is null)
        {
            adminRole = new IdentityRole<Guid> { Name = "Admin" };
            var created = await roleManager.CreateAsync(adminRole);
            if (!created.Succeeded)
            {
                _logger.LogWarning("Failed to create Admin role: {Errors}", created.Errors);
                return;
            }
        }

        await SyncRolePermissionsAsync(adminRole, dbContext, cancellationToken);

        if (!_environment.IsDevelopment())
        {
            return;
        }

        const string adminEmail = "admin@local.test";
        const string adminPassword = "Password123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createdUser = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createdUser.Succeeded)
            {
                _logger.LogWarning("Failed to create admin user: {Errors}", createdUser.Errors);
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, adminRole.Name!))
        {
            await userManager.AddToRoleAsync(adminUser, adminRole.Name!);
        }
    }

    private static async Task SyncRolePermissionsAsync(
        IdentityRole<Guid> adminRole,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var permissionKeys = AuthServerDefinitions.Permissions.Select(permission => permission.Key).ToArray();
        var permissions = await dbContext.Permissions
            .Where(permission => permissionKeys.Contains(permission.Key))
            .ToListAsync(cancellationToken);

        var existingPermissions = await dbContext.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == adminRole.Id)
            .Select(rolePermission => rolePermission.PermissionId)
            .ToListAsync(cancellationToken);

        var missingPermissions = permissions
            .Where(permission => !existingPermissions.Contains(permission.Id))
            .ToList();

        if (missingPermissions.Count == 0)
        {
            return;
        }

        foreach (var permission in missingPermissions)
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
