namespace AuthServer.Options;

public sealed class AuthServerUiOptions
{
    public const string SectionName = "AuthServer";

    public string UiMode { get; set; } = "Separate";

    public string UiBaseUrl { get; set; } = "http://localhost:5173";

    public string? HostedUiPath { get; set; }

    public bool IsHosted => string.Equals(UiMode, "Hosted", StringComparison.OrdinalIgnoreCase);

    public string GetHostedUiPath(string contentRootPath)
    {
        if (!string.IsNullOrWhiteSpace(HostedUiPath))
        {
            return Path.GetFullPath(HostedUiPath);
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, "..", "Web", "dist"));
    }
}
