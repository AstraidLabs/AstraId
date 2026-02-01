namespace AuthServer.Models;

public sealed record LoginRequest(string EmailOrUsername, string Password, string? ReturnUrl);

public sealed record RegisterRequest(string Email, string Password, string ConfirmPassword, string? ReturnUrl);

public sealed record AuthResponse(bool Success, string? RedirectTo, string? Error);

public sealed record AuthSessionResponse(
    bool IsAuthenticated,
    string? UserId,
    string? Email,
    string? UserName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
