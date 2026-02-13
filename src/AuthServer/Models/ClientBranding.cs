namespace AuthServer.Models;

public sealed record ClientBranding(
    string? DisplayName,
    string? LogoUrl,
    string? HomeUrl,
    string? PrivacyUrl,
    string? TermsUrl);
