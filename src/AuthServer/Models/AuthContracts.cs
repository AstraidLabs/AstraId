namespace AuthServer.Models;

public sealed record LoginRequest(string EmailOrUsername, string Password, string? ReturnUrl);

public sealed record RegisterRequest(string Email, string Password, string ConfirmPassword, string? ReturnUrl);

public sealed record LoginResponse(
    bool Success,
    string? RedirectTo,
    string? Error,
    bool RequiresTwoFactor = false,
    string? MfaToken = null);

public sealed record AuthResponse(bool Success, string? RedirectTo, string? Error, string? Message = null);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword, string ConfirmPassword);

public sealed record ResendActivationRequest(string Email);

public sealed record ActivateAccountRequest(string Email, string Token);

public sealed record MfaLoginRequest(string MfaToken, string Code, bool RememberMachine, bool UseRecoveryCode);

public sealed record MfaStatusResponse(bool Enabled, bool HasAuthenticatorKey, int RecoveryCodesLeft);

public sealed record MfaSetupResponse(string SharedKey, string OtpAuthUri, string QrCodeSvg);

public sealed record MfaConfirmRequest(string Code);

public sealed record MfaDisableRequest(string Code);

public sealed record MfaRecoveryCodesResponse(IReadOnlyCollection<string> RecoveryCodes);

public sealed record AuthSessionResponse(
    bool IsAuthenticated,
    string? UserId,
    string? Email,
    string? UserName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword,
    bool SignOutOtherSessions);

public sealed record ChangeEmailRequest(string NewEmail, string? ReturnUrl, string CurrentPassword);

public sealed record ConfirmEmailChangeRequest(Guid UserId, string NewEmail, string Token);

public sealed record RevokeSessionsRequest(string CurrentPassword);

public sealed record SecurityOverviewResponse(
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    int RecoveryCodesLeft,
    bool HasAuthenticatorKey,
    string? Email,
    string? UserName);

public sealed record MeSummaryResponse(
    string UserId,
    string? Email,
    string? UserName,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    IReadOnlyCollection<string> Roles,
    DateTimeOffset? CreatedUtc,
    DateTimeOffset? LastLoginUtc);

public sealed record ChangePasswordSelfRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);

public sealed record ChangeEmailStartRequest(string NewEmail, string Password);

public sealed record ConfirmEmailChangeSelfRequest(Guid UserId, string NewEmail, string Token);

public sealed record SignOutAllSessionsResponse(bool Success, string Message);

public sealed record UserSecurityEventResponse(
    Guid Id,
    DateTime TimestampUtc,
    string EventType,
    string? IpAddress,
    string? UserAgent,
    string? ClientId,
    string? TraceId);
