using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.Admin.Validation;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Admin;

public sealed class AdminClientService : IAdminClientService
{
    private static readonly IReadOnlyDictionary<string, string> GrantTypeReverseMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [OpenIddictConstants.GrantTypes.AuthorizationCode] = "authorization_code",
        [OpenIddictConstants.GrantTypes.RefreshToken] = "refresh_token",
        [OpenIddictConstants.GrantTypes.ClientCredentials] = "client_credentials"
    };

    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminClientService(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResult<AdminClientListItem>> GetClientsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var items = new List<AdminClientListItem>();
        var clientStates = await _dbContext.ClientStates
            .AsNoTracking()
            .ToDictionaryAsync(state => state.ApplicationId, cancellationToken);

        await foreach (var application in _applicationManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var clientId = await _applicationManager.GetClientIdAsync(application, cancellationToken) ?? string.Empty;
            var displayName = await _applicationManager.GetDisplayNameAsync(application, cancellationToken);

            if (!string.IsNullOrWhiteSpace(search)
                && !clientId.Contains(search, StringComparison.OrdinalIgnoreCase)
                && !(displayName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                continue;
            }

            var id = await _applicationManager.GetIdAsync(application, cancellationToken) ?? string.Empty;
            var clientType = await _applicationManager.GetClientTypeAsync(application, cancellationToken) ?? string.Empty;
            var enabled = ResolveEnabled(id, clientStates);

            items.Add(new AdminClientListItem(
                id,
                clientId,
                displayName,
                clientType,
                enabled));
        }

        var totalCount = items.Count;
        var pagedItems = items
            .OrderBy(item => item.ClientId, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<AdminClientListItem>(pagedItems, totalCount, page, pageSize);
    }

    public async Task<AdminClientDetail?> GetClientAsync(string id, CancellationToken cancellationToken)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        return application is null ? null : await BuildDetailAsync(application, cancellationToken);
    }

    public async Task<AdminClientSecretResult> CreateClientAsync(AdminClientCreateRequest request, CancellationToken cancellationToken)
    {
        var config = await NormalizeAsync(request, cancellationToken);
        var existing = await _applicationManager.FindByClientIdAsync(config.ClientId, cancellationToken);
        if (existing is not null)
        {
            var errors = new AdminValidationErrors();
            errors.Add("clientId", "Client ID already exists.");
            throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = config.ClientId,
            DisplayName = config.DisplayName,
            ClientType = config.ClientType
        };

        foreach (var uri in config.RedirectUris)
        {
            descriptor.RedirectUris.Add(uri);
        }

        foreach (var uri in config.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(uri);
        }

        foreach (var permission in BuildPermissions(config))
        {
            descriptor.Permissions.Add(permission);
        }

        foreach (var requirement in BuildRequirements(config))
        {
            descriptor.Requirements.Add(requirement);
        }

        string? clientSecret = null;
        if (string.Equals(config.ClientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.Ordinal))
        {
            clientSecret = GenerateSecret();
            descriptor.ClientSecret = clientSecret;
        }

        await _applicationManager.CreateAsync(descriptor, cancellationToken);

        var application = await _applicationManager.FindByClientIdAsync(config.ClientId, cancellationToken)
            ?? throw new InvalidOperationException("Created client was not found.");

        var applicationId = await _applicationManager.GetIdAsync(application, cancellationToken) ?? string.Empty;
        await UpsertClientStateAsync(applicationId, config.Enabled, cancellationToken);

        var detail = await BuildDetailAsync(application, cancellationToken);
        await LogAuditAsync("client.created", "OpenIddictApplication", detail.Id, new
        {
            detail.ClientId,
            detail.DisplayName,
            detail.ClientType,
            detail.GrantTypes,
            detail.Scopes,
            detail.Enabled
        });

        return new AdminClientSecretResult(detail, clientSecret);
    }

    public async Task<AdminClientDetail?> UpdateClientAsync(string id, AdminClientUpdateRequest request, CancellationToken cancellationToken)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var config = await NormalizeAsync(request, cancellationToken);
        var existing = await _applicationManager.FindByClientIdAsync(config.ClientId, cancellationToken);
        if (existing is not null)
        {
            var existingId = await _applicationManager.GetIdAsync(existing, cancellationToken);
            if (!string.Equals(existingId, id, StringComparison.Ordinal))
            {
                var errors = new AdminValidationErrors();
                errors.Add("clientId", "Client ID already exists.");
                throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
            }
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);

        descriptor.ClientId = config.ClientId;
        descriptor.DisplayName = config.DisplayName;
        descriptor.ClientType = config.ClientType;

        descriptor.RedirectUris.Clear();
        foreach (var uri in config.RedirectUris)
        {
            descriptor.RedirectUris.Add(uri);
        }

        descriptor.PostLogoutRedirectUris.Clear();
        foreach (var uri in config.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(uri);
        }

        descriptor.Permissions.Clear();
        foreach (var permission in BuildPermissions(config))
        {
            descriptor.Permissions.Add(permission);
        }

        descriptor.Requirements.Clear();
        foreach (var requirement in BuildRequirements(config))
        {
            descriptor.Requirements.Add(requirement);
        }

        if (string.Equals(config.ClientType, OpenIddictConstants.ClientTypes.Public, StringComparison.Ordinal))
        {
            descriptor.ClientSecret = null;
        }

        await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);

        var applicationId = await _applicationManager.GetIdAsync(application, cancellationToken) ?? string.Empty;
        await UpsertClientStateAsync(applicationId, config.Enabled, cancellationToken);

        var detail = await BuildDetailAsync(application, cancellationToken);
        await LogAuditAsync("client.updated", "OpenIddictApplication", detail.Id, new
        {
            detail.ClientId,
            detail.DisplayName,
            detail.ClientType,
            detail.GrantTypes,
            detail.Scopes,
            detail.Enabled
        });

        return detail;
    }

    public async Task<AdminClientSecretResult?> RotateSecretAsync(string id, CancellationToken cancellationToken)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var clientType = await _applicationManager.GetClientTypeAsync(application, cancellationToken);
        if (!string.Equals(clientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.Ordinal))
        {
            var errors = new AdminValidationErrors();
            errors.Add("clientType", "Client secret rotation is only supported for confidential clients.");
            throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);

        var newSecret = GenerateSecret();
        descriptor.ClientSecret = newSecret;

        await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);

        var detail = await BuildDetailAsync(application, cancellationToken);
        await LogAuditAsync("client.secret.rotated", "OpenIddictApplication", detail.Id, new
        {
            detail.ClientId
        });

        return new AdminClientSecretResult(detail, newSecret);
    }

    public async Task<AdminClientDetail?> SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var applicationId = await _applicationManager.GetIdAsync(application, cancellationToken) ?? string.Empty;
        await UpsertClientStateAsync(applicationId, enabled, cancellationToken);

        var detail = await BuildDetailAsync(application, cancellationToken);
        await LogAuditAsync(enabled ? "client.enabled" : "client.disabled", "OpenIddictApplication", detail.Id, new
        {
            detail.ClientId,
            detail.Enabled
        });

        return detail;
    }

    public async Task<bool> DeleteClientAsync(string id, CancellationToken cancellationToken)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return false;
        }

        var detail = await BuildDetailAsync(application, cancellationToken);
        await _applicationManager.DeleteAsync(application, cancellationToken);

        var existingState = await _dbContext.ClientStates
            .FirstOrDefaultAsync(state => state.ApplicationId == id, cancellationToken);
        if (existingState is not null)
        {
            _dbContext.ClientStates.Remove(existingState);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await LogAuditAsync("client.deleted", "OpenIddictApplication", detail.Id, new
        {
            detail.ClientId,
            detail.DisplayName
        });

        return true;
    }

    public async Task<IReadOnlyList<AdminClientScopeItem>> GetScopesAsync(CancellationToken cancellationToken)
    {
        var scopes = new List<AdminClientScopeItem>();

        await foreach (var scope in _scopeManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var name = await _scopeManager.GetNameAsync(scope, cancellationToken);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var displayName = await _scopeManager.GetDisplayNameAsync(scope, cancellationToken);
            scopes.Add(new AdminClientScopeItem(name, displayName));
        }

        return scopes
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<AdminClientDetail> BuildDetailAsync(object application, CancellationToken cancellationToken)
    {
        var id = await _applicationManager.GetIdAsync(application, cancellationToken) ?? string.Empty;
        var clientId = await _applicationManager.GetClientIdAsync(application, cancellationToken) ?? string.Empty;
        var displayName = await _applicationManager.GetDisplayNameAsync(application, cancellationToken);
        var clientType = await _applicationManager.GetClientTypeAsync(application, cancellationToken) ?? string.Empty;
        var enabled = await GetEnabledAsync(id, cancellationToken);
        var permissions = await _applicationManager.GetPermissionsAsync(application, cancellationToken);
        var requirements = await _applicationManager.GetRequirementsAsync(application, cancellationToken);
        var redirectUris = await _applicationManager.GetRedirectUrisAsync(application, cancellationToken);
        var postLogoutUris = await _applicationManager.GetPostLogoutRedirectUrisAsync(application, cancellationToken);

        var grantTypes = ParseGrantTypes(permissions);
        var scopes = ParseScopes(permissions);
        var pkceRequired = requirements.Contains(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange, StringComparer.Ordinal);

        return new AdminClientDetail(
            id,
            clientId,
            displayName,
            clientType,
            enabled,
            grantTypes,
            pkceRequired,
            scopes,
            redirectUris.Select(uri => uri.ToString()).ToArray(),
            postLogoutUris.Select(uri => uri.ToString()).ToArray());
    }

    private async Task<AdminClientConfiguration> NormalizeAsync(AdminClientCreateRequest request, CancellationToken cancellationToken)
    {
        return await NormalizeAsync(
            request.ClientId,
            request.DisplayName,
            request.ClientType,
            request.Enabled,
            request.GrantTypes,
            request.PkceRequired,
            request.Scopes,
            request.RedirectUris,
            request.PostLogoutRedirectUris,
            cancellationToken);
    }

    private async Task<AdminClientConfiguration> NormalizeAsync(AdminClientUpdateRequest request, CancellationToken cancellationToken)
    {
        return await NormalizeAsync(
            request.ClientId,
            request.DisplayName,
            request.ClientType,
            request.Enabled,
            request.GrantTypes,
            request.PkceRequired,
            request.Scopes,
            request.RedirectUris,
            request.PostLogoutRedirectUris,
            cancellationToken);
    }

    private async Task<AdminClientConfiguration> NormalizeAsync(
        string? clientId,
        string? displayName,
        string? clientType,
        bool enabled,
        IReadOnlyList<string> grantTypes,
        bool pkceRequired,
        IReadOnlyList<string> scopes,
        IReadOnlyList<string> redirectUris,
        IReadOnlyList<string> postLogoutRedirectUris,
        CancellationToken cancellationToken)
    {
        var errors = new AdminValidationErrors();

        var normalizedClientId = OidcValidationSpec.NormalizeClientId(clientId, errors, "clientId");
        var normalizedClientType = OidcValidationSpec.NormalizeClientType(clientType, errors, "clientType");
        var isPublic = string.Equals(normalizedClientType, OpenIddictConstants.ClientTypes.Public, StringComparison.Ordinal);

        var normalizedGrantTypes = OidcValidationSpec.NormalizeGrantTypes(grantTypes, errors, "grantTypes");
        var normalizedPkceRequired = pkceRequired
            || (isPublic && normalizedGrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode));

        OidcValidationSpec.ValidatePkceRules(isPublic, normalizedGrantTypes, normalizedPkceRequired, errors);

        var normalizedRedirectUris = OidcValidationSpec.NormalizeRedirectUris(
            redirectUris,
            errors,
            "redirectUris",
            "Redirect URI");
        if (normalizedGrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode)
            && normalizedRedirectUris.Count == 0)
        {
            errors.Add("redirectUris", "Redirect URIs are required for authorization_code.");
        }

        var normalizedPostLogoutUris = OidcValidationSpec.NormalizeRedirectUris(
            postLogoutRedirectUris,
            errors,
            "postLogoutRedirectUris",
            "Post logout redirect URI");

        var normalizedScopes = scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var scope in normalizedScopes)
        {
            var scopeEntity = await _scopeManager.FindByNameAsync(scope, cancellationToken);
            if (scopeEntity is null)
            {
                errors.Add("scopes", $"Unknown scope: {scope}.");
            }
        }

        if (errors.HasErrors)
        {
            throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
        }

        return new AdminClientConfiguration(
            normalizedClientId!,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            normalizedClientType,
            enabled,
            normalizedGrantTypes.ToList(),
            normalizedPkceRequired,
            normalizedScopes,
            normalizedRedirectUris,
            normalizedPostLogoutUris);
    }


    private static IEnumerable<string> BuildPermissions(AdminClientConfiguration config)
    {
        var permissions = new HashSet<string>
        {
            OpenIddictConstants.Permissions.Endpoints.Token
        };

        if (config.RedirectUris.Count > 0 || config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode))
        {
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
        }

        if (config.PostLogoutRedirectUris.Count > 0)
        {
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
        }

        if (config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode)
            || config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.RefreshToken))
        {
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);
        }

        if (config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode))
        {
            permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
        }

        if (config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.RefreshToken))
        {
            permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
        }

        if (config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.ClientCredentials))
        {
            permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
        }

        foreach (var scope in config.Scopes)
        {
            permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        return permissions;
    }

    private static IEnumerable<string> BuildRequirements(AdminClientConfiguration config)
    {
        if (config.PkceRequired)
        {
            return [OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange];
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> ParseGrantTypes(IReadOnlyList<string> permissions)
    {
        var grantTypes = new List<string>();

        if (permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode, StringComparer.Ordinal))
        {
            grantTypes.Add(GrantTypeReverseMap[OpenIddictConstants.GrantTypes.AuthorizationCode]);
        }

        if (permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.RefreshToken, StringComparer.Ordinal))
        {
            grantTypes.Add(GrantTypeReverseMap[OpenIddictConstants.GrantTypes.RefreshToken]);
        }

        if (permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials, StringComparer.Ordinal))
        {
            grantTypes.Add(GrantTypeReverseMap[OpenIddictConstants.GrantTypes.ClientCredentials]);
        }

        return grantTypes;
    }

    private static IReadOnlyList<string> ParseScopes(IReadOnlyList<string> permissions)
    {
        return permissions
            .Where(permission => permission.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope, StringComparison.Ordinal))
            .Select(permission => permission.Substring(OpenIddictConstants.Permissions.Prefixes.Scope.Length))
            .ToList();
    }

    private static bool ResolveEnabled(string applicationId, IReadOnlyDictionary<string, ClientState> states)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return true;
        }

        return states.TryGetValue(applicationId, out var state) ? state.Enabled : true;
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private async Task<bool> GetEnabledAsync(string applicationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return true;
        }

        var state = await _dbContext.ClientStates
            .AsNoTracking()
            .FirstOrDefaultAsync(clientState => clientState.ApplicationId == applicationId, cancellationToken);

        return state?.Enabled ?? true;
    }

    private async Task UpsertClientStateAsync(string applicationId, bool enabled, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return;
        }

        var state = await _dbContext.ClientStates
            .FirstOrDefaultAsync(clientState => clientState.ApplicationId == applicationId, cancellationToken);

        if (state is null)
        {
            _dbContext.ClientStates.Add(new ClientState
            {
                ApplicationId = applicationId,
                Enabled = enabled,
                UpdatedUtc = DateTime.UtcNow
            });
        }
        else
        {
            state.Enabled = enabled;
            state.UpdatedUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task LogAuditAsync(string action, string targetType, string targetId, object data)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = GetActorUserId(),
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = JsonSerializer.Serialize(data)
        });

        await _dbContext.SaveChangesAsync();
    }

    private Guid? GetActorUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private sealed record AdminClientConfiguration(
        string ClientId,
        string? DisplayName,
        string ClientType,
        bool Enabled,
        IReadOnlyList<string> GrantTypes,
        bool PkceRequired,
        IReadOnlyList<string> Scopes,
        IReadOnlyList<Uri> RedirectUris,
        IReadOnlyList<Uri> PostLogoutRedirectUris);
}
