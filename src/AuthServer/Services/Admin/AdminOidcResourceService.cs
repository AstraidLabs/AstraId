using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.Admin.Validation;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Admin;

/// <summary>
/// Provides admin oidc resource service functionality.
/// </summary>
public sealed class AdminOidcResourceService : IAdminOidcResourceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminOidcResourceService(
        ApplicationDbContext dbContext,
        IOpenIddictScopeManager scopeManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _scopeManager = scopeManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResult<AdminOidcResourceListItem>> GetResourcesAsync(
        string? search,
        int page,
        int pageSize,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var query = _dbContext.OidcResources.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(resource => resource.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(resource =>
                resource.Name.Contains(search) ||
                (resource.DisplayName ?? string.Empty).Contains(search) ||
                (resource.Description ?? string.Empty).Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(resource => resource.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(resource => new AdminOidcResourceListItem(
                resource.Id,
                resource.Name,
                resource.DisplayName,
                resource.Description,
                resource.IsActive,
                resource.CreatedUtc,
                resource.UpdatedUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminOidcResourceListItem>(items, totalCount, page, pageSize);
    }

    public async Task<AdminOidcResourceDetail?> GetResourceAsync(Guid id, CancellationToken cancellationToken)
    {
        var resource = await _dbContext.OidcResources.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return resource is null ? null : MapDetail(resource);
    }

    public async Task<AdminOidcResourceUsage?> GetResourceUsageAsync(Guid id, CancellationToken cancellationToken)
    {
        var resource = await _dbContext.OidcResources.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (resource is null)
        {
            return null;
        }

        var scopeCount = await CountScopesForResourceAsync(resource.Name, cancellationToken);
        return new AdminOidcResourceUsage(scopeCount);
    }

    public async Task<AdminOidcResourceDetail> CreateResourceAsync(AdminOidcResourceRequest request, CancellationToken cancellationToken)
    {
        var errors = new AdminValidationErrors();
        var name = OidcValidationSpec.NormalizeResourceName(request.Name, errors, "name");
        if (errors.HasErrors)
        {
            throw new AdminValidationException("Invalid resource configuration.", errors.ToDictionary());
        }

        if (await _dbContext.OidcResources.AnyAsync(item => item.Name == name, cancellationToken))
        {
            errors.Add("name", "Resource name already exists.");
            throw new AdminValidationException("Invalid resource configuration.", errors.ToDictionary());
        }

        var now = DateTime.UtcNow;
        var resource = new OidcResource
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = NormalizeOptional(request.DisplayName),
            Description = NormalizeOptional(request.Description),
            IsActive = request.IsActive,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        _dbContext.OidcResources.Add(resource);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("resource.created", "OidcResource", resource.Id.ToString(), resource);
        return MapDetail(resource);
    }

    public async Task<AdminOidcResourceDetail?> UpdateResourceAsync(Guid id, AdminOidcResourceRequest request, CancellationToken cancellationToken)
    {
        var resource = await _dbContext.OidcResources.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (resource is null)
        {
            return null;
        }

        var errors = new AdminValidationErrors();
        var name = OidcValidationSpec.NormalizeResourceName(request.Name, errors, "name");
        if (errors.HasErrors)
        {
            throw new AdminValidationException("Invalid resource configuration.", errors.ToDictionary());
        }

        if (!string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            if (await _dbContext.OidcResources.AnyAsync(item => item.Name == name && item.Id != id, cancellationToken))
            {
                errors.Add("name", "Resource name already exists.");
                throw new AdminValidationException("Invalid resource configuration.", errors.ToDictionary());
            }

            var inUse = await IsResourceInUseAsync(resource.Name, cancellationToken);
            if (inUse)
            {
                errors.Add("name", "Resource name cannot be changed while it is assigned to scopes.");
                throw new AdminValidationException("Invalid resource configuration.", errors.ToDictionary());
            }
        }

        resource.Name = name;
        resource.DisplayName = NormalizeOptional(request.DisplayName);
        resource.Description = NormalizeOptional(request.Description);
        resource.IsActive = request.IsActive;
        resource.UpdatedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("resource.updated", "OidcResource", resource.Id.ToString(), resource);
        return MapDetail(resource);
    }

    public async Task<bool> DeleteResourceAsync(Guid id, CancellationToken cancellationToken)
    {
        var resource = await _dbContext.OidcResources.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (resource is null)
        {
            return false;
        }

        var inUse = await IsResourceInUseAsync(resource.Name, cancellationToken);
        if (inUse)
        {
            var errors = new AdminValidationErrors();
            errors.Add("resource", "Resource is assigned to one or more scopes. Remove it from scopes before deleting.");
            throw new AdminValidationException("Resource is in use.", errors.ToDictionary());
        }

        resource.IsActive = false;
        resource.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("resource.deleted", "OidcResource", resource.Id.ToString(), new { resource.Name });
        return true;
    }

    private async Task<bool> IsResourceInUseAsync(string resourceName, CancellationToken cancellationToken)
    {
        await foreach (var scope in _scopeManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var resources = await _scopeManager.GetResourcesAsync(scope, cancellationToken);
            if (resources.Contains(resourceName, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<int> CountScopesForResourceAsync(string resourceName, CancellationToken cancellationToken)
    {
        var count = 0;
        await foreach (var scope in _scopeManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var resources = await _scopeManager.GetResourcesAsync(scope, cancellationToken);
            if (resources.Contains(resourceName, StringComparer.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static AdminOidcResourceDetail MapDetail(OidcResource resource)
    {
        return new AdminOidcResourceDetail(
            resource.Id,
            resource.Name,
            resource.DisplayName,
            resource.Description,
            resource.IsActive,
            resource.CreatedUtc,
            resource.UpdatedUtc);
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
