namespace AuthServer.Services.Admin.Models;

public sealed record AdminSessionInfo(
    Guid UserId,
    string? Email,
    string? UserName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
