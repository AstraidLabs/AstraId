using AuthServer.Data;
using AuthServer.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Apis.Endpoints;

[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly IAdminEndpointService _endpointService;

    public CreateModel(IAdminEndpointService endpointService)
    {
        _endpointService = endpointService;
    }

    [BindProperty(SupportsGet = true, Name = "apiId")]
    public Guid ApiId { get; set; }

    [BindProperty]
    public ApiEndpoint Endpoint { get; set; } = new();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Endpoint.ApiResourceId = ApiId;
        await _endpointService.CreateEndpointAsync(Endpoint, cancellationToken);
        return RedirectToPage("Index", new { apiId = ApiId });
    }
}
