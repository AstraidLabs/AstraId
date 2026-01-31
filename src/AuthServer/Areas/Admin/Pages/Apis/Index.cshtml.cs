using AuthServer.Data;
using AuthServer.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Apis;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IAdminApiResourceService _apiResourceService;

    public IndexModel(IAdminApiResourceService apiResourceService)
    {
        _apiResourceService = apiResourceService;
    }

    public IReadOnlyList<ApiResource> ApiResources { get; private set; } = Array.Empty<ApiResource>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ApiResources = await _apiResourceService.GetApiResourcesAsync(cancellationToken);
    }
}
