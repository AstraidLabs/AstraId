using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Admin;

public sealed class AdminOidcScopeService : IAdminOidcScopeService
{
    private static readonly Regex ScopeNameRegex = new("^[a-z0-9:_\\.-]+$", RegexOptions.Compiled);
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

    public async Task<AdminOidcScopeDetail> CreateScopeAsync(AdminOidcScopeRequest request, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name ?? string.Empty);
        ValidateName(name);

        var existing = await _scopeManager.FindByNameAsync(name, cancellationToken);
        if (existing is not null)
        {
            throw new AdminOidcValidationException("Invalid scope configuration.", "Scope name already exists.");
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

        var name = NormalizeName(request.Name ?? string.Empty);
        ValidateName(name);

        var existing = await _scopeManager.FindByNameAsync(name, cancellationToken);
        if (existing is not null)
        {
            var existingId = await _scopeManager.GetIdAsync(existing, cancellationToken);
            var scopeId = await _scopeManager.GetIdAsync(scope, cancellationToken);
            if (!string.Equals(existingId, scopeId, StringComparison.Ordinal))
            {
                throw new AdminOidcValidationException("Invalid scope configuration.", "Scope name already exists.");
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
            throw new AdminOidcValidationException(
                "Scope is in use.",
                "Scope is assigned to one or more clients. Remove it from clients before deleting.");
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
            throw new AdminOidcValidationException(
                "Invalid scope configuration.",
                $"Unknown resources: {string.Join(", ", invalid)}.");
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

    private static string NormalizeName(string name)
    {
        return name.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AdminOidcValidationException("Invalid scope configuration.", "Scope name is required.");
        }

        if (!ScopeNameRegex.IsMatch(name))
        {
            throw new AdminOidcValidationException(
                "Invalid scope configuration.",
                "Scope name must match [a-z0-9:_\\.-]+.");
        }
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
