namespace AuthServer.Services.Admin;

/// <summary>
/// Provides client application types functionality.
/// </summary>
public static class ClientApplicationTypes
{
    public const string Web = "web";
    public const string Mobile = "mobile";
    public const string Desktop = "desktop";
    public const string Integration = "integration";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Web,
        Mobile,
        Desktop,
        Integration
    };
}
