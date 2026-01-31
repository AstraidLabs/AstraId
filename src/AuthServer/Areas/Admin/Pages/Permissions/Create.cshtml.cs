using AuthServer.Data;
using AuthServer.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Permissions;

[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly IAdminPermissionAdminService _permissionService;

    public CreateModel(IAdminPermissionAdminService permissionService)
    {
        _permissionService = permissionService;
    }

    [BindProperty]
    public Permission Permission { get; set; } = new();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _permissionService.CreatePermissionAsync(Permission, cancellationToken);
        return RedirectToPage("Index");
    }
}
