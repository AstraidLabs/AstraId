namespace AuthServer.Services.Admin.Models;

public sealed record AdminUserCreateRequest(
    string Email,
    string? UserName,
    string? PhoneNumber,
    string? Password);
