using AuthServer.Data;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/api-resources")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminApiResourcesController : ControllerBase
{
    private readonly IAdminApiResourceService _apiResourceService;
    private readonly IAdminEndpointService _endpointService;
    private readonly ApplicationDbContext _dbContext;

    public AdminApiResourcesController(
        IAdminApiResourceService apiResourceService,
        IAdminEndpointService endpointService,
        ApplicationDbContext dbContext)
    {
        _apiResourceService = apiResourceService;
        _endpointService = endpointService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminApiResourceListItem>>> GetResources(CancellationToken cancellationToken)
    {
        var resources = await _apiResourceService.GetApiResourcesAsync(cancellationToken);
        var items = resources
            .Select(resource => new AdminApiResourceListItem(
                resource.Id,
                resource.Name,
                resource.DisplayName,
                resource.BaseUrl,
                resource.IsActive))
            .ToList();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminApiResourceDetail>> GetResource(Guid id, CancellationToken cancellationToken)
    {
        var resource = await _apiResourceService.GetApiResourceAsync(id, cancellationToken);
        if (resource is null)
        {
            return NotFound();
        }

        return Ok(new AdminApiResourceDetail(
            resource.Id,
            resource.Name,
            resource.DisplayName,
            resource.BaseUrl,
            resource.IsActive,
            null));
    }

    [HttpPost]
    public async Task<ActionResult<AdminApiResourceDetail>> CreateResource(
        [FromBody] AdminApiResourceRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        var baseUrl = request.BaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(displayName))
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Invalid API resource.",
                Detail = "Name and display name are required."
            });
        }

        if (await _dbContext.ApiResources.AnyAsync(resource => resource.Name == name, cancellationToken))
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "API resource already exists.",
                Detail = $"API resource '{name}' already exists."
            });
        }

        var resource = new ApiResource
        {
            Name = name,
            DisplayName = displayName,
            BaseUrl = baseUrl,
            IsActive = request.IsActive
        };

        var created = await _apiResourceService.CreateApiResourceAsync(resource, cancellationToken);
        var response = new AdminApiResourceDetail(
            created.Id,
            created.Name,
            created.DisplayName,
            created.BaseUrl,
            created.IsActive,
            null);

        return CreatedAtAction(nameof(GetResource), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminApiResourceDetail>> UpdateResource(
        Guid id,
        [FromBody] AdminApiResourceRequest request,
        CancellationToken cancellationToken)
    {
        var resource = await _apiResourceService.GetApiResourceAsync(id, cancellationToken);
        if (resource is null)
        {
            return NotFound();
        }

        var name = request.Name?.Trim() ?? string.Empty;
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        var baseUrl = request.BaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(displayName))
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "Invalid API resource.",
                Detail = "Name and display name are required."
            });
        }

        if (!string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase)
            && await _dbContext.ApiResources.AnyAsync(item => item.Name == name && item.Id != id, cancellationToken))
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Title = "API resource already exists.",
                Detail = $"API resource '{name}' already exists."
            });
        }

        resource.Name = name;
        resource.DisplayName = displayName;
        resource.BaseUrl = baseUrl;
        resource.IsActive = request.IsActive;

        await _apiResourceService.UpdateApiResourceAsync(resource, cancellationToken);

        return Ok(new AdminApiResourceDetail(
            resource.Id,
            resource.Name,
            resource.DisplayName,
            resource.BaseUrl,
            resource.IsActive,
            null));
    }

    [HttpPost("{id:guid}/rotate-key")]
    public async Task<ActionResult<AdminApiResourceDetail>> RotateKey(Guid id, CancellationToken cancellationToken)
    {
        var (apiKey, resource) = await _apiResourceService.RotateApiKeyAsync(id, cancellationToken);
        return Ok(new AdminApiResourceDetail(
            resource.Id,
            resource.Name,
            resource.DisplayName,
            resource.BaseUrl,
            resource.IsActive,
            apiKey));
    }

    [HttpGet("{id:guid}/endpoints")]
    public async Task<ActionResult<IReadOnlyList<AdminApiEndpointListItem>>> GetEndpoints(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resource = await _apiResourceService.GetApiResourceAsync(id, cancellationToken);
        if (resource is null)
        {
            return NotFound();
        }

        var endpoints = await _endpointService.GetEndpointsAsync(id, cancellationToken);
        var items = endpoints.Select(endpoint =>
        {
            var permissionIds = endpoint.EndpointPermissions.Select(item => item.PermissionId).ToList();
            var permissionKeys = endpoint.EndpointPermissions
                .Select(item => item.Permission?.Key)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key!)
                .Distinct()
                .ToList();

            return new AdminApiEndpointListItem(
                endpoint.Id,
                endpoint.Method,
                endpoint.Path,
                endpoint.DisplayName,
                endpoint.IsDeprecated,
                endpoint.IsActive,
                permissionIds,
                permissionKeys);
        }).ToList();

        return Ok(items);
    }

    [HttpPut("{id:guid}/endpoints/{endpointId:guid}/permissions")]
    public async Task<IActionResult> UpdateEndpointPermissions(
        Guid id,
        Guid endpointId,
        [FromBody] AdminApiEndpointPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var endpoint = await _dbContext.ApiEndpoints.FirstOrDefaultAsync(item => item.Id == endpointId, cancellationToken);
        if (endpoint is null || endpoint.ApiResourceId != id)
        {
            return NotFound();
        }

        await _endpointService.SetEndpointPermissionsAsync(endpointId, request.PermissionIds, cancellationToken);
        return NoContent();
    }
}
