namespace AuthServer.Services.Admin.Models;

public sealed record AdminEncryptionKeyStatus(
    bool Enabled,
    string Source,
    string? Thumbprint,
    string? Subject,
    DateTime? NotBeforeUtc,
    DateTime? NotAfterUtc);
