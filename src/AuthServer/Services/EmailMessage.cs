namespace AuthServer.Services;

/// <summary>
/// Provides email message functionality.
/// </summary>
public sealed record EmailMessage(
    string ToEmail,
    string? ToName,
    string Subject,
    string HtmlBody,
    string? TextBody = null);
