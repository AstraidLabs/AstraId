namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for session management.
/// </summary>
public sealed class SessionManagementOptions
{
    public const string SectionName = "AuthServer:SessionManagement";

    public bool BackChannelEnabled { get; set; } = false;
    public bool FrontChannelEnabled { get; set; } = false;
    public Dictionary<string, string> BackChannelLogoutUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
