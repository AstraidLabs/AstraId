using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/oidc/scopes")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminScopesController : ControllerBase
{
    private readonly IAdminOidcScopeService _scopeService;

    public AdminScopesController(IAdminOidcScopeService scopeService)
    {
        _scopeService = scopeService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminOidcScopeListItem>>> GetScopes(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var scopes = await _scopeService.GetScopesAsync(search, page, pageSize, cancellationToken);
        return Ok(scopes);
    }

    [HttpGet("{nameOrId}")]
    public async Task<ActionResult<AdminOidcScopeDetail>> GetScope(string nameOrId, CancellationToken cancellationToken)
    {
        var scope = await _scopeService.GetScopeAsync(nameOrId, cancellationToken);
        return scope is null ? NotFound() : Ok(scope);
    }

    [HttpPost]
    public async Task<ActionResult<AdminOidcScopeDetail>> CreateScope(
        [FromBody] AdminOidcScopeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var scope = await _scopeService.CreateScopeAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetScope), new { nameOrId = scope.Id }, scope);
        }
        catch (AdminOidcValidationException exception)
        {
            return ValidationProblem(exception.ToProblemDetails("scope"));
        }
    }

    [HttpPut("{nameOrId}")]
    public async Task<ActionResult<AdminOidcScopeDetail>> UpdateScope(
        string nameOrId,
        [FromBody] AdminOidcScopeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var scope = await _scopeService.UpdateScopeAsync(nameOrId, request, cancellationToken);
            return scope is null ? NotFound() : Ok(scope);
        }
        catch (AdminOidcValidationException exception)
        {
            return ValidationProblem(exception.ToProblemDetails("scope"));
        }
    }

    [HttpDelete("{nameOrId}")]
    public async Task<IActionResult> DeleteScope(string nameOrId, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _scopeService.DeleteScopeAsync(nameOrId, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (AdminOidcValidationException exception)
        {
            return ValidationProblem(exception.ToProblemDetails("scope"));
        }
    }
}
