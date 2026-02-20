using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Admin;

/// <summary>
/// Provides admin api resource service functionality.
/// </summary>
public sealed class AdminApiResourceService : IAdminApiResourceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminApiResourceService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<ApiResource>> GetApiResourcesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ApiResources
            .OrderBy(resource => resource.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiResource?> GetApiResourceAsync(Guid apiResourceId, CancellationToken cancellationToken)
    {
        return await _dbContext.ApiResources.FirstOrDefaultAsync(resource => resource.Id == apiResourceId, cancellationToken);
    }

    public async Task<ApiResource?> GetApiResourceByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await _dbContext.ApiResources.FirstOrDefaultAsync(resource => resource.Name == name, cancellationToken);
    }

    public async Task<ApiResource> CreateApiResourceAsync(ApiResource apiResource, CancellationToken cancellationToken)
    {
        apiResource.Id = Guid.NewGuid();
        apiResource.CreatedUtc = DateTime.UtcNow;
        apiResource.UpdatedUtc = apiResource.CreatedUtc;
        _dbContext.ApiResources.Add(apiResource);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("api-resource.created", "ApiResource", apiResource.Id.ToString(), apiResource);
        return apiResource;
    }

    public async Task UpdateApiResourceAsync(ApiResource apiResource, CancellationToken cancellationToken)
    {
        apiResource.UpdatedUtc = DateTime.UtcNow;
        _dbContext.ApiResources.Update(apiResource);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("api-resource.updated", "ApiResource", apiResource.Id.ToString(), apiResource);
    }

    public async Task<ApiResource> RotateApiKeyAsync(Guid apiResourceId, CancellationToken cancellationToken)
    {
        var resource = await _dbContext.ApiResources.FirstOrDefaultAsync(item => item.Id == apiResourceId, cancellationToken);
        if (resource is null)
        {
            throw new InvalidOperationException("API resource not found.");
        }

        var apiKey = ApiKeyHasher.GenerateApiKey();
        resource.ApiKeyHash = ApiKeyHasher.HashApiKey(apiKey);
        resource.UpdatedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync("api-resource.api-key.rotated", "ApiResource", resource.Id.ToString(), new { resource.Name });

        return resource;
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
