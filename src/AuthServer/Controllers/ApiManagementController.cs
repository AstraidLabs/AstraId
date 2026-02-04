using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services;
using AuthServer.Services.Admin;
using AuthServer.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers;

[ApiController]
[Route("admin")]
public class ApiManagementController : ControllerBase
{
    private readonly IAdminApiResourceService _apiResourceService;
    private readonly IAdminEndpointService _endpointService;
    private readonly ApplicationDbContext _dbContext;

    public ApiManagementController(
        IAdminApiResourceService apiResourceService,
        IAdminEndpointService endpointService,
        ApplicationDbContext dbContext)
    {
        _apiResourceService = apiResourceService;
        _endpointService = endpointService;
        _dbContext = dbContext;
    }

    [HttpPost("api-sync/{apiName}/endpoints")]
    public async Task<IActionResult> SyncEndpoints(
        string apiName,
        [FromBody] List<ApiEndpointSyncDto> endpoints,
        CancellationToken cancellationToken)
    {
        var apiResource = await _apiResourceService.GetApiResourceByNameAsync(apiName, cancellationToken);
        if (apiResource is null)
        {
            return NotFound();
        }

        if (!TryAuthorizeApiKey(apiResource))
        {
            return Unauthorized();
        }

        var validation = AdminValidation.ValidateEndpointSync(endpoints);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid endpoint payload.").ApplyDefaults(HttpContext));
        }

        var result = await _endpointService.SyncEndpointsAsync(apiResource, endpoints, cancellationToken);
        return Ok(result);
    }

    [HttpGet("apis/{apiName}/policy-map")]
    public async Task<IActionResult> GetPolicyMap(string apiName, CancellationToken cancellationToken)
    {
        var apiResource = await _apiResourceService.GetApiResourceByNameAsync(apiName, cancellationToken);
        if (apiResource is null)
        {
            return NotFound();
        }

        if (!TryAuthorizeApiKey(apiResource))
        {
            return Unauthorized();
        }

        var entries = await _dbContext.ApiEndpoints
            .Include(endpoint => endpoint.EndpointPermissions)
            .ThenInclude(endpointPermission => endpointPermission.Permission)
            .Where(endpoint => endpoint.ApiResourceId == apiResource.Id && endpoint.IsActive)
            .OrderBy(endpoint => endpoint.Path)
            .ThenBy(endpoint => endpoint.Method)
            .Select(endpoint => new ApiPolicyMapEntryDto(
                endpoint.Method,
                endpoint.Path,
                endpoint.EndpointPermissions
                    .Select(endpointPermission => endpointPermission.Permission!.Key)
                    .Distinct()
                    .ToArray()))
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }

    private bool TryAuthorizeApiKey(ApiResource apiResource)
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
        {
            return false;
        }

        return ApiKeyHasher.VerifyApiKey(apiKey.ToString(), apiResource.ApiKeyHash);
    }
}
