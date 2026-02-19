namespace AuthServer.Options;

public sealed class SessionManagementOptions
{
    public const string SectionName = "AuthServer:SessionManagement";

    public bool BackChannelEnabled { get; set; } = false;
    public bool FrontChannelEnabled { get; set; } = false;
    public Dictionary<string, string> BackChannelLogoutUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
