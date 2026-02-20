using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Admin;

/// <summary>
/// Provides admin endpoint service functionality.
/// </summary>
public sealed class AdminEndpointService : IAdminEndpointService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminEndpointService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<ApiEndpoint>> GetEndpointsAsync(Guid apiResourceId, CancellationToken cancellationToken)
    {
        return await _dbContext.ApiEndpoints
            .Include(endpoint => endpoint.EndpointPermissions)
            .ThenInclude(endpointPermission => endpointPermission.Permission)
            .Where(endpoint => endpoint.ApiResourceId == apiResourceId)
            .OrderBy(endpoint => endpoint.Path)
            .ThenBy(endpoint => endpoint.Method)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken cancellationToken)
    {
        return await _dbContext.ApiEndpoints
            .Include(endpoint => endpoint.EndpointPermissions)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
    }

    public async Task<ApiEndpoint> CreateEndpointAsync(ApiEndpoint endpoint, CancellationToken cancellationToken)
    {
        endpoint.Id = Guid.NewGuid();
        endpoint.CreatedUtc = DateTime.UtcNow;
        endpoint.UpdatedUtc = endpoint.CreatedUtc;
        endpoint.Method = NormalizeMethod(endpoint.Method);
        endpoint.Path = NormalizePath(endpoint.Path);
        _dbContext.ApiEndpoints.Add(endpoint);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("api-endpoint.created", "ApiEndpoint", endpoint.Id.ToString(), endpoint);
        return endpoint;
    }

    public async Task UpdateEndpointAsync(ApiEndpoint endpoint, CancellationToken cancellationToken)
    {
        endpoint.Method = NormalizeMethod(endpoint.Method);
        endpoint.Path = NormalizePath(endpoint.Path);
        endpoint.UpdatedUtc = DateTime.UtcNow;
        _dbContext.ApiEndpoints.Update(endpoint);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("api-endpoint.updated", "ApiEndpoint", endpoint.Id.ToString(), endpoint);
    }

    public async Task SetEndpointPermissionsAsync(Guid endpointId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.EndpointPermissions
            .Where(endpointPermission => endpointPermission.EndpointId == endpointId)
            .ToListAsync(cancellationToken);

        var desired = permissionIds.Distinct().ToHashSet();
        var toRemove = existing.Where(item => !desired.Contains(item.PermissionId)).ToList();
        var existingIds = existing.Select(item => item.PermissionId).ToHashSet();

        foreach (var remove in toRemove)
        {
            _dbContext.EndpointPermissions.Remove(remove);
        }

        foreach (var permissionId in desired.Where(id => !existingIds.Contains(id)))
        {
            _dbContext.EndpointPermissions.Add(new EndpointPermission
            {
                EndpointId = endpointId,
                PermissionId = permissionId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("api-endpoint.permissions.updated", "ApiEndpoint", endpointId.ToString(), new { permissionIds = desired.ToArray() });
    }

    public async Task<IReadOnlyList<Guid>> GetEndpointPermissionIdsAsync(Guid endpointId, CancellationToken cancellationToken)
    {
        return await _dbContext.EndpointPermissions
            .Where(endpointPermission => endpointPermission.EndpointId == endpointId)
            .Select(endpointPermission => endpointPermission.PermissionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<EndpointSyncResult> SyncEndpointsAsync(ApiResource apiResource, IReadOnlyCollection<ApiEndpointSyncDto> endpoints, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var existing = await _dbContext.ApiEndpoints
            .Where(endpoint => endpoint.ApiResourceId == apiResource.Id)
            .ToListAsync(cancellationToken);

        var created = 0;
        var updated = 0;
        var payloadKeys = new HashSet<(string method, string path)>();

        foreach (var dto in endpoints)
        {
            var method = NormalizeMethod(dto.Method);
            var path = NormalizePath(dto.Path);
            payloadKeys.Add((method, path));

            var match = existing.FirstOrDefault(item =>
                NormalizeMethod(item.Method) == method &&
                NormalizePath(item.Path) == path);

            if (match is null)
            {
                var entity = new ApiEndpoint
                {
                    Id = Guid.NewGuid(),
                    ApiResourceId = apiResource.Id,
                    Method = method,
                    Path = path,
                    DisplayName = dto.DisplayName,
                    IsDeprecated = dto.Deprecated ?? false,
                    IsActive = true,
                    Tags = dto.Tags,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };

                _dbContext.ApiEndpoints.Add(entity);
                created++;
                continue;
            }

            match.Method = method;
            match.Path = path;
            match.DisplayName = dto.DisplayName;
            match.IsDeprecated = dto.Deprecated ?? match.IsDeprecated;
            match.Tags = dto.Tags;
            match.IsActive = true;
            match.UpdatedUtc = now;
            updated++;
        }

        var deactivated = 0;
        foreach (var endpoint in existing)
        {
            if (!payloadKeys.Contains((NormalizeMethod(endpoint.Method), NormalizePath(endpoint.Path))) && endpoint.IsActive)
            {
                endpoint.IsActive = false;
                endpoint.UpdatedUtc = now;
                deactivated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await LogAuditAsync("api-endpoint.sync", "ApiResource", apiResource.Id.ToString(), new
        {
            apiResource.Name,
            created,
            updated,
            deactivated
        });

        return new EndpointSyncResult(created, updated, deactivated);
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

    private static string NormalizeMethod(string method)
    {
        return string.IsNullOrWhiteSpace(method) ? string.Empty : method.Trim().ToUpperInvariant();
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var trimmed = path.Trim();
        return trimmed.Length > 1 && trimmed.EndsWith("/", StringComparison.Ordinal)
            ? trimmed.TrimEnd('/')
            : trimmed;
    }
}
