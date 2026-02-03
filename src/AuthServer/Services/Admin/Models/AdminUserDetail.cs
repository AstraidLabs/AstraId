namespace AuthServer.Services.Admin.Models;

public sealed record AdminUserDetail(
    Guid Id,
    string? Email,
    string? UserName,
    string? PhoneNumber,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool IsLockedOut,
    bool IsActive,
    IReadOnlyList<string> Roles);
