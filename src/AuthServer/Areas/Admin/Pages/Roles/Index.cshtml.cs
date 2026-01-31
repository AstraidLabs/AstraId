using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Roles;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IAdminRoleService _adminRoleService;

    public IndexModel(IAdminRoleService adminRoleService)
    {
        _adminRoleService = adminRoleService;
    }

    public IReadOnlyList<AdminRoleListItem> Roles { get; private set; } = Array.Empty<AdminRoleListItem>();

    [BindProperty]
    public string? NewRoleName { get; set; }

    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Roles = await _adminRoleService.GetRolesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NewRoleName))
        {
            ModelState.AddModelError(string.Empty, "Role name is required.");
            await OnGetAsync(cancellationToken);
            return Page();
        }

        var result = await _adminRoleService.CreateRoleAsync(NewRoleName.Trim());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, string.Join("; ", result.Errors.Select(error => error.Description)));
        }
        else
        {
            StatusMessage = "Role created.";
        }

        Roles = await _adminRoleService.GetRolesAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await _adminRoleService.GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        var result = await _adminRoleService.DeleteRoleAsync(role);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, string.Join("; ", result.Errors.Select(error => error.Description)));
        }
        else
        {
            StatusMessage = "Role deleted.";
        }

        Roles = await _adminRoleService.GetRolesAsync(cancellationToken);
        return Page();
    }
}
