using AuthServer.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Apis;

[Authorize(Policy = "AdminOnly")]
public class ApiKeyModel : PageModel
{
    private readonly IAdminApiResourceService _apiResourceService;

    public ApiKeyModel(IAdminApiResourceService apiResourceService)
    {
        _apiResourceService = apiResourceService;
    }

    [BindProperty(SupportsGet = true, Name = "id")]
    public Guid ApiResourceId { get; set; }

    public string? ApiName { get; private set; }
    public string? GeneratedApiKey { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var resource = await _apiResourceService.GetApiResourceAsync(ApiResourceId, cancellationToken);
        if (resource is null)
        {
            return NotFound();
        }

        ApiName = resource.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await _apiResourceService.RotateApiKeyAsync(ApiResourceId, cancellationToken);
        ApiName = result.Resource.Name;
        GeneratedApiKey = result.ApiKey;
        return Page();
    }
}
