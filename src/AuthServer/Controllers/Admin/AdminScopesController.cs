using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/scopes")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminScopesController : ControllerBase
{
    private readonly IAdminClientService _clientService;

    public AdminScopesController(IAdminClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminClientScopeItem>>> GetScopes(CancellationToken cancellationToken)
    {
        var scopes = await _clientService.GetScopesAsync(cancellationToken);
        return Ok(scopes);
    }
}
