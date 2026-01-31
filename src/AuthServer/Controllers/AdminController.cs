using AuthServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthServer.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ApplicationDbContext _dbContext;

    public AdminController(
        IOpenIddictApplicationManager applicationManager,
        ApplicationDbContext dbContext)
    {
        _applicationManager = applicationManager;
        _dbContext = dbContext;
    }

    [HttpGet("clients")]
    public async Task<IActionResult> GetClients(CancellationToken cancellationToken)
    {
        var clients = new List<object>();

        await foreach (var application in _applicationManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var clientId = await _applicationManager.GetClientIdAsync(application, cancellationToken);
            var displayName = await _applicationManager.GetDisplayNameAsync(application, cancellationToken);
            var redirectUris = await _applicationManager.GetRedirectUrisAsync(application, cancellationToken);

            clients.Add(new
            {
                clientId,
                displayName,
                redirectUris = redirectUris.Select(uri => uri.ToString()).ToArray()
            });
        }

        return Ok(clients);
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.Permissions
            .OrderBy(permission => permission.Group)
            .ThenBy(permission => permission.Key)
            .Select(permission => new
            {
                permission.Key,
                permission.Description,
                permission.Group,
                permission.IsSystem
            })
            .ToListAsync(cancellationToken);

        return Ok(permissions);
    }
}
