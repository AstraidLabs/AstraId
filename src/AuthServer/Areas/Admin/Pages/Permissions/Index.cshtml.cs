using AuthServer.Data;
using AuthServer.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Permissions;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IAdminPermissionAdminService _permissionService;

    public IndexModel(IAdminPermissionAdminService permissionService)
    {
        _permissionService = permissionService;
    }

    public IReadOnlyList<Permission> Permissions { get; private set; } = Array.Empty<Permission>();

    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Permissions = await _permissionService.GetPermissionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _permissionService.DeletePermissionAsync(id, cancellationToken);
        StatusMessage = "Permission deleted.";
        Permissions = await _permissionService.GetPermissionsAsync(cancellationToken);
        return Page();
    }
}
