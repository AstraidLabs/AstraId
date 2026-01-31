namespace AuthServer.Services.Admin.Models;

public sealed record AdminUserListItem(
    Guid Id,
    string? Email,
    string? UserName,
    bool EmailConfirmed,
    bool IsLockedOut);
