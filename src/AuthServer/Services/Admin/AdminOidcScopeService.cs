using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.Admin.Validation;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Admin;

/// <summary>
/// Provides admin oidc scope service functionality.
/// </summary>
public sealed class AdminOidcScopeService : IAdminOidcScopeService
{
    private const string DescriptionProperty = "description";
    private const string ClaimsProperty = "claims";

    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminOidcScopeService(
        IOpenIddictScopeManager scopeManager,
        IOpenIddictApplicationManager applicationManager,
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _scopeManager = scopeManager;
        _applicationManager = applicationManager;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResult<AdminOidcScopeListItem>> GetScopesAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var items = new List<AdminOidcScopeListItem>();
        await foreach (var scope in _scopeManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var name = await _scopeManager.GetNameAsync(scope, cancellationToken);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var displayName = await _scopeManager.GetDisplayNameAsync(scope, cancellationToken);
            var resources = await _scopeManager.GetResourcesAsync(scope, cancellationToken);
            var claims = await GetClaimsFromPropertiesAsync(scope, cancellationToken);
            var description = await GetDescriptionAsync(scope, cancellationToken);

            if (!string.IsNullOrWhiteSpace(search)
                && !(name.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || (displayName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                     || (description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)))
            {
                continue;
            }

            var id = await _scopeManager.GetIdAsync(scope, cancellationToken) ?? string.Empty;
            items.Add(new AdminOidcScopeListItem(
                id,
                name,
                displayName,
                description,
                resources.ToList(),
                claims.ToList()));
        }

        var totalCount = items.Count;
        var paged = items
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<AdminOidcScopeListItem>(paged, totalCount, page, pageSize);
    }

    public async Task<AdminOidcScopeDetail?> GetScopeAsync(string nameOrId, CancellationToken cancellationToken)
    {
        var scope = await _scopeManager.FindByIdAsync(nameOrId, cancellationToken)
            ?? await _scopeManager.FindByNameAsync(nameOrId, cancellationToken);

        if (scope is null)
        {
            return null;
        }

        return await BuildDetailAsync(scope, cancellationToken);
    }

    public async Task<AdminOidcScopeUsage?> GetScopeUsageAsync(string nameOrId, CancellationToken cancellationToken)
    {
        var scope = await _scopeManager.FindByIdAsync(nameOrId, cancellationToken)
            ?? await _scopeManager.FindByNameAsync(nameOrId, cancellationToken);

        if (scope is null)
        {
            return null;
        }

        var name = await _scopeManager.GetNameAsync(scope, cancellationToken);
        if (string.IsNullOrWhiteSpace(name))
        {
            return new AdminOidcScopeUsage(0);
        }

        var clientCount = await CountClientsForScopeAsync(name, cancellationToken);
        return new AdminOidcScopeUsage(clientCount);
    }

    public async Task<AdminOidcScopeDetail> CreateScopeAsync(AdminOidcScopeRequest request, CancellationToken cancellationToken)
    {
        var errors = new AdminValidationErrors();
        var name = OidcValidationSpec.NormalizeScopeName(request.Name, errors, "name");
        if (errors.HasErrors)
        {
            throw new AdminValidationException("Invalid scope configuration.", errors.ToDictionary());
        }

        var existing = await _scopeManager.FindByNameAsync(name, cancellationToken);
        if (existing is not null)
        {
            errors.Add("name", "Scope name already exists.");
            throw new AdminValidationException("Invalid scope configuration.", errors.ToDictionary());
        }

        var resources = await NormalizeResourcesAsync(request.Resources ?? Array.Empty<string>(), cancellationToken);
        var claims = NormalizeList(request.Claims ?? Array.Empty<string>());

        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = name,
            DisplayName = NormalizeOptional(request.DisplayName)
        };

        SetDescription(descriptor, request.Description);

        foreach (var resource in resources)
        {
            descriptor.Resources.Add(resource);
        }

        SetClaims(descriptor, claims);

        await _scopeManager.CreateAsync(descriptor, cancellationToken);

        var scope = await _scopeManager.FindByNameAsync(name, cancellationToken)
            ?? throw new InvalidOperationException("Created scope not found.");

        var detail = await BuildDetailAsync(scope, cancellationToken);
        await LogAuditAsync("scope.created", "OpenIddictScope", detail.Id, new
        {
            detail.Name,
            detail.DisplayName,
            detail.Description,
            detail.Resources,
            detail.Claims
        });

        return detail;
    }

    public async Task<AdminOidcScopeDetail?> UpdateScopeAsync(string nameOrId, AdminOidcScopeRequest request, CancellationToken cancellationToken)
    {
        var scope = await _scopeManager.FindByIdAsync(nameOrId, cancellationToken)
            ?? await _scopeManager.FindByNameAsync(nameOrId, cancellationToken);

        if (scope is null)
        {
            return null;
        }

        var errors = new AdminValidationErrors();
        var name = OidcValidationSpec.NormalizeScopeName(request.Name, errors, "name");
        if (errors.HasErrors)
        {
            throw new AdminValidationException("Invalid scope configuration.", errors.ToDictionary());
        }

        var existing = await _scopeManager.FindByNameAsync(name, cancellationToken);
        if (existing is not null)
        {
            var existingId = await _scopeManager.GetIdAsync(existing, cancellationToken);
            var scopeId = await _scopeManager.GetIdAsync(scope, cancellationToken);
            if (!string.Equals(existingId, scopeId, StringComparison.Ordinal))
            {
                errors.Add("name", "Scope name already exists.");
                throw new AdminValidationException("Invalid scope configuration.", errors.ToDictionary());
            }
        }

        var resources = await NormalizeResourcesAsync(request.Resources ?? Array.Empty<string>(), cancellationToken);
        var claims = NormalizeList(request.Claims ?? Array.Empty<string>());

        var descriptor = new OpenIddictScopeDescriptor();
        await _scopeManager.PopulateAsync(descriptor, scope, cancellationToken);

        descriptor.Name = name;
        descriptor.DisplayName = NormalizeOptional(request.DisplayName);

        descriptor.Resources.Clear();
        foreach (var resource in resources)
        {
            descriptor.Resources.Add(resource);
        }

        SetClaims(descriptor, claims);

        SetDescription(descriptor, request.Description);

        await _scopeManager.UpdateAsync(scope, descriptor, cancellationToken);

        var detail = await BuildDetailAsync(scope, cancellationToken);
        await LogAuditAsync("scope.updated", "OpenIddictScope", detail.Id, new
        {
            detail.Name,
            detail.DisplayName,
            detail.Description,
            detail.Resources,
            detail.Claims
        });

        return detail;
    }

    public async Task<bool> DeleteScopeAsync(string nameOrId, CancellationToken cancellationToken)
    {
        var scope = await _scopeManager.FindByIdAsync(nameOrId, cancellationToken)
            ?? await _scopeManager.FindByNameAsync(nameOrId, cancellationToken);

        if (scope is null)
        {
            return false;
        }

        var name = await _scopeManager.GetNameAsync(scope, cancellationToken);
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (await IsScopeAssignedToClientAsync(name, cancellationToken))
        {
            var errors = new AdminValidationErrors();
            errors.Add("scope", "Scope is assigned to one or more clients. Remove it from clients before deleting.");
            throw new AdminValidationException("Scope is in use.", errors.ToDictionary());
        }

        var id = await _scopeManager.GetIdAsync(scope, cancellationToken) ?? string.Empty;
        await _scopeManager.DeleteAsync(scope, cancellationToken);
        await LogAuditAsync("scope.deleted", "OpenIddictScope", id, new { name });
        return true;
    }

    private async Task<AdminOidcScopeDetail> BuildDetailAsync(object scope, CancellationToken cancellationToken)
    {
        var id = await _scopeManager.GetIdAsync(scope, cancellationToken) ?? string.Empty;
        var name = await _scopeManager.GetNameAsync(scope, cancellationToken) ?? string.Empty;
        var displayName = await _scopeManager.GetDisplayNameAsync(scope, cancellationToken);
        var resources = await _scopeManager.GetResourcesAsync(scope, cancellationToken);
        var claims = await GetClaimsFromPropertiesAsync(scope, cancellationToken);
        var description = await GetDescriptionAsync(scope, cancellationToken);

        return new AdminOidcScopeDetail(
            id,
            name,
            displayName,
            description,
            resources.ToList(),
            claims.ToList());
    }

    private async Task<IReadOnlyList<string>> NormalizeResourcesAsync(
        IReadOnlyList<string> resources,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeList(resources);
        if (normalized.Count == 0)
        {
            return normalized;
        }

        var activeResources = await _dbContext.OidcResources
            .AsNoTracking()
            .Where(resource => resource.IsActive)
            .Select(resource => resource.Name)
            .ToListAsync(cancellationToken);

        var activeSet = activeResources.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var invalid = normalized.Where(resource => !activeSet.Contains(resource)).ToList();
        if (invalid.Count > 0)
        {
            var errors = new AdminValidationErrors();
            errors.Add("resources", $"Unknown resources: {string.Join(", ", invalid)}.");
            throw new AdminValidationException("Invalid scope configuration.", errors.ToDictionary());
        }

        return normalized;
    }

    private static List<string> NormalizeList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => value!)
            .ToList();
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private async Task<bool> IsScopeAssignedToClientAsync(string scopeName, CancellationToken cancellationToken)
    {
        await foreach (var application in _applicationManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var permissions = await _applicationManager.GetPermissionsAsync(application, cancellationToken);
            if (permissions.Contains($"{OpenIddictConstants.Permissions.Prefixes.Scope}{scopeName}", StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<int> CountClientsForScopeAsync(string scopeName, CancellationToken cancellationToken)
    {
        var count = 0;
        await foreach (var application in _applicationManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var permissions = await _applicationManager.GetPermissionsAsync(application, cancellationToken);
            if (permissions.Contains($"{OpenIddictConstants.Permissions.Prefixes.Scope}{scopeName}", StringComparer.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    private async Task<string?> GetDescriptionAsync(object scope, CancellationToken cancellationToken)
    {
        var properties = await _scopeManager.GetPropertiesAsync(scope, cancellationToken);
        if (properties.TryGetValue(DescriptionProperty, out var element)
            && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        return null;
    }

    private static void SetDescription(OpenIddictScopeDescriptor descriptor, string? description)
    {
        descriptor.Properties.Remove(DescriptionProperty);
        var normalized = NormalizeOptional(description);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            descriptor.Properties[DescriptionProperty] = JsonSerializer.SerializeToElement(normalized);
        }
    }

    private async Task<IReadOnlyList<string>> GetClaimsFromPropertiesAsync(object scope, CancellationToken cancellationToken)
    {
        var properties = await _scopeManager.GetPropertiesAsync(scope, cancellationToken);
        if (!properties.TryGetValue(ClaimsProperty, out var element)
            || element.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var claims = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(value);
            }
        }

        return NormalizeList(claims);
    }

    private static void SetClaims(OpenIddictScopeDescriptor descriptor, IReadOnlyList<string> claims)
    {
        descriptor.Properties.Remove(ClaimsProperty);
        if (claims.Count > 0)
        {
            descriptor.Properties[ClaimsProperty] = JsonSerializer.SerializeToElement(claims);
        }
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
}
