using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/oidc/resources")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminOidcResourcesController : ControllerBase
{
    private readonly IAdminOidcResourceService _resourceService;

    public AdminOidcResourcesController(IAdminOidcResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminOidcResourceListItem>>> GetResources(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var resources = await _resourceService.GetResourcesAsync(search, page, pageSize, includeInactive, cancellationToken);
        return Ok(resources);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminOidcResourceDetail>> GetResource(Guid id, CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetResourceAsync(id, cancellationToken);
        return resource is null ? NotFound() : Ok(resource);
    }

    [HttpPost]
    public async Task<ActionResult<AdminOidcResourceDetail>> CreateResource(
        [FromBody] AdminOidcResourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var resource = await _resourceService.CreateResourceAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, resource);
        }
        catch (AdminOidcValidationException exception)
        {
            return ValidationProblem(exception.ToProblemDetails("resource"));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminOidcResourceDetail>> UpdateResource(
        Guid id,
        [FromBody] AdminOidcResourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var resource = await _resourceService.UpdateResourceAsync(id, request, cancellationToken);
            return resource is null ? NotFound() : Ok(resource);
        }
        catch (AdminOidcValidationException exception)
        {
            return ValidationProblem(exception.ToProblemDetails("resource"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteResource(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _resourceService.DeleteResourceAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (AdminOidcValidationException exception)
        {
            return ValidationProblem(exception.ToProblemDetails("resource"));
        }
    }
}
