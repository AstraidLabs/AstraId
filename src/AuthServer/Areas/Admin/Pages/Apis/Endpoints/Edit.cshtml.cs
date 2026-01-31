using AuthServer.Data;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Apis.Endpoints;

[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly IAdminEndpointService _endpointService;
    private readonly IAdminPermissionAdminService _permissionService;

    public EditModel(IAdminEndpointService endpointService, IAdminPermissionAdminService permissionService)
    {
        _endpointService = endpointService;
        _permissionService = permissionService;
    }

    [BindProperty(SupportsGet = true, Name = "apiId")]
    public Guid ApiId { get; set; }

    [BindProperty(SupportsGet = true, Name = "endpointId")]
    public Guid EndpointId { get; set; }

    [BindProperty]
    public ApiEndpoint Endpoint { get; set; } = new();

    public IReadOnlyList<PermissionGroup> PermissionGroups { get; private set; } = Array.Empty<PermissionGroup>();

    [BindProperty]
    public List<Guid> SelectedPermissions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var endpoint = await _endpointService.GetEndpointAsync(EndpointId, cancellationToken);
        if (endpoint is null)
        {
            return NotFound();
        }

        Endpoint = endpoint;
        ApiId = endpoint.ApiResourceId;
        PermissionGroups = await _permissionService.GetGroupedPermissionsAsync(cancellationToken);
        SelectedPermissions = (await _endpointService.GetEndpointPermissionIdsAsync(endpoint.Id, cancellationToken)).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var endpoint = await _endpointService.GetEndpointAsync(EndpointId, cancellationToken);
        if (endpoint is null)
        {
            return NotFound();
        }

        endpoint.Method = Endpoint.Method;
        endpoint.Path = Endpoint.Path;
        endpoint.DisplayName = Endpoint.DisplayName;
        endpoint.Tags = Endpoint.Tags;
        endpoint.IsDeprecated = Endpoint.IsDeprecated;
        endpoint.IsActive = Endpoint.IsActive;

        await _endpointService.UpdateEndpointAsync(endpoint, cancellationToken);
        await _endpointService.SetEndpointPermissionsAsync(endpoint.Id, SelectedPermissions, cancellationToken);

        return RedirectToPage("Index", new { apiId = endpoint.ApiResourceId });
    }
}
