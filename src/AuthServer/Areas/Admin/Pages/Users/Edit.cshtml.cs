using AuthServer.Data;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Users;

[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly IAdminUserService _adminUserService;
    private readonly IAdminRoleService _adminRoleService;

    public EditModel(IAdminUserService adminUserService, IAdminRoleService adminRoleService)
    {
        _adminUserService = adminUserService;
        _adminRoleService = adminRoleService;
    }

    [BindProperty(SupportsGet = true, Name = "id")]
    public Guid UserId { get; set; }

    public string? Email { get; private set; }
    public string? UserName { get; private set; }
    public IReadOnlyList<AdminRoleListItem> AvailableRoles { get; private set; } = Array.Empty<AdminRoleListItem>();

    [BindProperty]
    public List<string> SelectedRoles { get; set; } = new();

    [BindProperty]
    public bool IsLockedOut { get; set; }

    [BindProperty]
    public string? NewPassword { get; set; }

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var user = await _adminUserService.GetUserAsync(UserId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        await PopulateAsync(user, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var user = await _adminUserService.GetUserAsync(UserId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var rolesResult = await _adminUserService.SetUserRolesAsync(user, SelectedRoles);
        if (!rolesResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, string.Join("; ", rolesResult.Errors.Select(error => error.Description)));
        }

        await _adminUserService.SetLockoutAsync(user, IsLockedOut, cancellationToken);

        if (!string.IsNullOrWhiteSpace(NewPassword))
        {
            var resetResult = await _adminUserService.ResetPasswordAsync(user, NewPassword);
            if (!resetResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, string.Join("; ", resetResult.Errors.Select(error => error.Description)));
            }
        }

        StatusMessage = "Saved.";
        await PopulateAsync(user, cancellationToken);
        return Page();
    }

    private async Task PopulateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        Email = user.Email;
        UserName = user.UserName;
        AvailableRoles = await _adminRoleService.GetRolesAsync(cancellationToken);
        SelectedRoles = (await _adminUserService.GetUserRolesAsync(user)).ToList();
        IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
    }
}
