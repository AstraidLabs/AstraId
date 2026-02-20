namespace Api.Options;

/// <summary>
/// Represents the available csp mode values.
/// </summary>
public enum CspMode
{
    Off,
    ReportOnly,
    Enforce
}

/// <summary>
/// Provides configuration options for security headers.
/// </summary>
public sealed class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    public bool Enabled { get; set; } = true;
    public bool EnableHsts { get; set; } = true;
    public CspMode CspMode { get; set; } = CspMode.Off;
    public string[] AllowedFrameAncestors { get; set; } = ["'none'"];
    public string[] AdditionalScriptSources { get; set; } = System.Array.Empty<string>();
}
