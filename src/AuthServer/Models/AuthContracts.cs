namespace AuthServer.Models;

public sealed record LoginRequest(string EmailOrUsername, string Password, string? ReturnUrl);

public sealed record RegisterRequest(string Email, string Password, string ConfirmPassword, string? ReturnUrl);

public sealed record AuthResponse(bool Success, string? RedirectTo, string? Error);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword, string ConfirmPassword);

public sealed record ResendActivationRequest(string Email);

public sealed record ActivateAccountRequest(string Email, string Token);

public sealed record AuthSessionResponse(
    bool IsAuthenticated,
    string? UserId,
    string? Email,
    string? UserName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
