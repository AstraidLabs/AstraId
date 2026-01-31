using AuthServer.Data;
using AuthServer.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Apis.Endpoints;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IAdminApiResourceService _apiResourceService;
    private readonly IAdminEndpointService _endpointService;

    public IndexModel(IAdminApiResourceService apiResourceService, IAdminEndpointService endpointService)
    {
        _apiResourceService = apiResourceService;
        _endpointService = endpointService;
    }

    [BindProperty(SupportsGet = true, Name = "apiId")]
    public Guid ApiId { get; set; }

    public string? ApiName { get; private set; }
    public IReadOnlyList<ApiEndpoint> Endpoints { get; private set; } = Array.Empty<ApiEndpoint>();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var apiResource = await _apiResourceService.GetApiResourceAsync(ApiId, cancellationToken);
        if (apiResource is null)
        {
            return NotFound();
        }

        ApiName = apiResource.Name;
        Endpoints = await _endpointService.GetEndpointsAsync(ApiId, cancellationToken);
        return Page();
    }
}
