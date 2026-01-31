using AuthServer.Data;
using AuthServer.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Apis;

[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly IAdminApiResourceService _apiResourceService;

    public EditModel(IAdminApiResourceService apiResourceService)
    {
        _apiResourceService = apiResourceService;
    }

    [BindProperty(SupportsGet = true, Name = "id")]
    public Guid ApiResourceId { get; set; }

    [BindProperty]
    public ApiResource ApiResource { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var resource = await _apiResourceService.GetApiResourceAsync(ApiResourceId, cancellationToken);
        if (resource is null)
        {
            return NotFound();
        }

        ApiResource = resource;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var resource = await _apiResourceService.GetApiResourceAsync(ApiResource.Id, cancellationToken);
        if (resource is null)
        {
            return NotFound();
        }

        resource.Name = ApiResource.Name;
        resource.DisplayName = ApiResource.DisplayName;
        resource.BaseUrl = ApiResource.BaseUrl;
        resource.IsActive = ApiResource.IsActive;

        await _apiResourceService.UpdateApiResourceAsync(resource, cancellationToken);
        return RedirectToPage("Index");
    }
}
