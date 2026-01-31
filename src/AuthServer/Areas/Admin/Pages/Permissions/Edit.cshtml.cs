using AuthServer.Data;
using AuthServer.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Permissions;

[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly IAdminPermissionAdminService _permissionService;

    public EditModel(IAdminPermissionAdminService permissionService)
    {
        _permissionService = permissionService;
    }

    [BindProperty(SupportsGet = true, Name = "id")]
    public Guid PermissionId { get; set; }

    [BindProperty]
    public Permission Permission { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var permission = await _permissionService.GetPermissionAsync(PermissionId, cancellationToken);
        if (permission is null)
        {
            return NotFound();
        }

        Permission = permission;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var existing = await _permissionService.GetPermissionAsync(Permission.Id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.IsSystem)
        {
            existing.Description = Permission.Description;
            existing.Group = Permission.Group;
        }
        else
        {
            existing.Key = Permission.Key;
            existing.Description = Permission.Description;
            existing.Group = Permission.Group;
            existing.IsSystem = Permission.IsSystem;
        }

        await _permissionService.UpdatePermissionAsync(existing, cancellationToken);
        return RedirectToPage("Index");
    }
}
