using AuthServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Areas.Admin.Pages;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public int UserCount { get; private set; }
    public int RoleCount { get; private set; }
    public int PermissionCount { get; private set; }
    public int EndpointCount { get; private set; }

    public async Task OnGetAsync()
    {
        UserCount = await _userManager.Users.CountAsync();
        RoleCount = await _roleManager.Roles.CountAsync();
        PermissionCount = await _dbContext.Permissions.CountAsync();
        EndpointCount = await _dbContext.ApiEndpoints.CountAsync();
    }
}
