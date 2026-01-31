using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Users;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private const int DefaultPageSize = 20;
    private readonly IAdminUserService _adminUserService;

    public IndexModel(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int Page { get; set; } = 1;

    public IReadOnlyList<AdminUserListItem> Users { get; private set; } = Array.Empty<AdminUserListItem>();
    public int TotalPages { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var result = await _adminUserService.GetUsersAsync(Search, Page, DefaultPageSize, cancellationToken);
        Users = result.Items;
        TotalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize);
    }
}
