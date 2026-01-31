using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Roles;

[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly IAdminRoleService _adminRoleService;
    private readonly IAdminPermissionAdminService _permissionService;

    public EditModel(IAdminRoleService adminRoleService, IAdminPermissionAdminService permissionService)
    {
        _adminRoleService = adminRoleService;
        _permissionService = permissionService;
    }

    [BindProperty(SupportsGet = true, Name = "id")]
    public Guid RoleId { get; set; }

    public string? RoleName { get; private set; }
    public IReadOnlyList<PermissionGroup> PermissionGroups { get; private set; } = Array.Empty<PermissionGroup>();

    [BindProperty]
    public List<Guid> SelectedPermissions { get; set; } = new();

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var role = await _adminRoleService.GetRoleAsync(RoleId, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        await PopulateAsync(role.Id, role.Name ?? string.Empty, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var role = await _adminRoleService.GetRoleAsync(RoleId, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        await _adminRoleService.SetRolePermissionsAsync(role.Id, SelectedPermissions, cancellationToken);
        StatusMessage = "Permissions updated.";
        await PopulateAsync(role.Id, role.Name ?? string.Empty, cancellationToken);
        return Page();
    }

    private async Task PopulateAsync(Guid roleId, string roleName, CancellationToken cancellationToken)
    {
        RoleName = roleName;
        PermissionGroups = await _permissionService.GetGroupedPermissionsAsync(cancellationToken);
        SelectedPermissions = (await _adminRoleService.GetRolePermissionIdsAsync(roleId, cancellationToken)).ToList();
    }
}
