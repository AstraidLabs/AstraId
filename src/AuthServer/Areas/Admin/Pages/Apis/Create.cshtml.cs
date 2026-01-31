using AuthServer.Data;
using AuthServer.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Apis;

[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly IAdminApiResourceService _apiResourceService;

    public CreateModel(IAdminApiResourceService apiResourceService)
    {
        _apiResourceService = apiResourceService;
    }

    [BindProperty]
    public ApiResource ApiResource { get; set; } = new();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _apiResourceService.CreateApiResourceAsync(ApiResource, cancellationToken);
        return RedirectToPage("Index");
    }
}
