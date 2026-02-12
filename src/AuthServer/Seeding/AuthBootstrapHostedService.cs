using System.Security.Cryptography;
using System.Text.Json;
using AuthServer.Services.Admin;
using AuthServer.Data;
using AuthServer.Options;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace AuthServer.Seeding;

public sealed class AuthBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IOptions<BootstrapAdminOptions> _bootstrapOptions;
    private readonly ILogger<AuthBootstrapHostedService> _logger;

    public AuthBootstrapHostedService(
        IServiceProvider serviceProvider,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IOptions<BootstrapAdminOptions> bootstrapOptions,
        ILogger<AuthBootstrapHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _environment = environment;
        _configuration = configuration;
        _bootstrapOptions = bootstrapOptions;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        await SyncPermissionsAsync(dbContext, cancellationToken);
        await SyncApiResourcesAsync(dbContext, cancellationToken);

        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await SyncScopesAsync(scopeManager, cancellationToken);
        await SyncApplicationsAsync(applicationManager, cancellationToken);
        await SyncClientStatesAsync(applicationManager, dbContext, cancellationToken);

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
                ClientType = client.Type
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
            currentDescriptor.ClientType = descriptor.ClientType;

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
           OpenIddictConstants.Permissions.Endpoints.EndSession
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
                case OpenIddictConstants.GrantTypes.ClientCredentials:
                    permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
                    break;
                case OpenIddictConstants.GrantTypes.Password:
                    permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
                    break;
            }
        }

        if (client.AllowedGrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode)
            || client.AllowedGrantTypes.Contains(OpenIddictConstants.GrantTypes.RefreshToken))
        {
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);
        }

        if (client.AllowIntrospection)
        {
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
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

    private static async Task SyncClientStatesAsync(
        IOpenIddictApplicationManager applicationManager,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var knownApplicationIds = new HashSet<string>(StringComparer.Ordinal);

        await foreach (var application in applicationManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var applicationId = await applicationManager.GetIdAsync(application, cancellationToken);
            if (!string.IsNullOrWhiteSpace(applicationId))
            {
                knownApplicationIds.Add(applicationId);
            }
        }

        if (knownApplicationIds.Count == 0)
        {
            return;
        }

        var existingStates = await dbContext.ClientStates
            .Where(state => knownApplicationIds.Contains(state.ApplicationId))
            .Select(state => state.ApplicationId)
            .ToListAsync(cancellationToken);

        var existingSet = existingStates.ToHashSet(StringComparer.Ordinal);
        var now = DateTime.UtcNow;

        foreach (var applicationId in knownApplicationIds)
        {
            if (existingSet.Contains(applicationId))
            {
                continue;
            }

            dbContext.ClientStates.Add(new ClientState
            {
                ApplicationId = applicationId,
                Enabled = true,
                SystemManaged = true,
                OverridesJson = JsonSerializer.Serialize(new
                {
                    clientApplicationType = ClientApplicationTypes.Web,
                    allowIntrospection = false,
                    allowUserCredentials = false,
                    allowedScopesForPasswordGrant = Array.Empty<string>()
                }),
                UpdatedUtc = now
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(
        IServiceProvider serviceProvider,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var options = _bootstrapOptions.Value;
        var hasBootstrapConfig = _configuration.GetSection(BootstrapAdminOptions.SectionName).Exists();
        var isDevelopment = _environment.IsDevelopment();

        if (options.OnlyInDevelopment && !isDevelopment)
        {
            return;
        }

        if (!hasBootstrapConfig && !isDevelopment)
        {
            return;
        }

        if (hasBootstrapConfig && !options.Enabled)
        {
            return;
        }

        var roleName = string.IsNullOrWhiteSpace(options.RoleName) ? "Admin" : options.RoleName;

        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var adminRole = await roleManager.FindByNameAsync(roleName);
        if (adminRole is null)
        {
            adminRole = new IdentityRole<Guid> { Name = roleName };
            var created = await roleManager.CreateAsync(adminRole);
            if (!created.Succeeded)
            {
                _logger.LogWarning("Failed to create Admin role: {Errors}", created.Errors);
                return;
            }

            await LogAuditAsync(
                dbContext,
                "role.created",
                "Role",
                adminRole.Id.ToString(),
                new { roleName },
                cancellationToken);
        }

        await SyncRolePermissionsAsync(adminRole, dbContext, cancellationToken);

        var adminEmail = options.Email;
        var adminPassword = options.Password;

        if (!hasBootstrapConfig && isDevelopment)
        {
            adminEmail ??= "admin@local.test";
            adminPassword ??= "Password123!";
        }

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            _logger.LogWarning("Bootstrap admin user skipped because Email is not configured.");
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                if (!options.GeneratePasswordWhenMissing)
                {
                    _logger.LogWarning("Bootstrap admin user skipped because Password is not configured.");
                    return;
                }

                adminPassword = GeneratePassword();
                _logger.LogInformation("Generated bootstrap admin password for {Email}.", adminEmail);
            }

            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = options.RequireConfirmedEmail,
                IsActive = true
            };

            var createdUser = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createdUser.Succeeded)
            {
                _logger.LogWarning("Failed to create admin user: {Errors}", createdUser.Errors);
                return;
            }

            await LogAuditAsync(
                dbContext,
                "user.admin.created",
                "User",
                adminUser.Id.ToString(),
                new { email = adminEmail },
                cancellationToken);

            if (!hasBootstrapConfig && isDevelopment)
            {
                _logger.LogInformation("Bootstrap admin user created in development.");
            }
            else
            {
                _logger.LogInformation("Bootstrap admin user created.");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, roleName))
        {
            var result = await userManager.AddToRoleAsync(adminUser, roleName);
            if (result.Succeeded)
            {
                await LogAuditAsync(
                    dbContext,
                    "user.role.assigned",
                    "User",
                    adminUser.Id.ToString(),
                    new { roleName },
                    cancellationToken);
            }
        }
    }

    private static async Task SyncApiResourcesAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ApiResources.ToListAsync(cancellationToken);

        foreach (var definition in AuthServerDefinitions.ApiResourceSeeds)
        {
            var resource = existing.FirstOrDefault(item => item.Name == definition.Name);
            if (resource is null)
            {
                dbContext.ApiResources.Add(new ApiResource
                {
                    Id = Guid.NewGuid(),
                    Name = definition.Name,
                    DisplayName = definition.DisplayName,
                    BaseUrl = definition.BaseUrl,
                    IsActive = definition.IsActive,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow
                });
                continue;
            }

            resource.DisplayName = definition.DisplayName;
            resource.BaseUrl = definition.BaseUrl;
            resource.IsActive = definition.IsActive;
            resource.UpdatedUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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

    private static async Task LogAuditAsync(
        ApplicationDbContext dbContext,
        string action,
        string targetType,
        string? targetId,
        object data,
        CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = JsonSerializer.Serialize(data)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GeneratePassword(int length = 16)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string symbols = "!@#$%^&*_-+=";

        var required = new[]
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            symbols[RandomNumberGenerator.GetInt32(symbols.Length)]
        };

        var allChars = upper + lower + digits + symbols;
        var buffer = new char[length];
        var remaining = length - required.Length;

        for (var i = 0; i < remaining; i++)
        {
            buffer[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        Array.Copy(required, 0, buffer, remaining, required.Length);

        for (var i = buffer.Length - 1; i > 0; i--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(i + 1);
            (buffer[i], buffer[swapIndex]) = (buffer[swapIndex], buffer[i]);
        }

        return new string(buffer);
    }
}
