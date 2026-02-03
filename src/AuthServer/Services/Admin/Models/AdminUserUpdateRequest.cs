namespace AuthServer.Services.Admin.Models;

public sealed record AdminUserUpdateRequest(
    string Email,
    string? UserName,
    string? PhoneNumber,
    bool EmailConfirmed,
    bool IsActive);
