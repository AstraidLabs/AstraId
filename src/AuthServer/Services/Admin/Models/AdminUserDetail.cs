namespace AuthServer.Services.Admin.Models;

public sealed record AdminUserDetail(
    Guid Id,
    string? Email,
    string? UserName,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool IsLockedOut,
    IReadOnlyList<string> Roles);
