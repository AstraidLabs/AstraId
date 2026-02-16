namespace Api.Options;

public enum CspMode
{
    Off,
    ReportOnly,
    Enforce
}

public sealed class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    public bool Enabled { get; set; } = true;
    public bool EnableHsts { get; set; } = true;
    public CspMode CspMode { get; set; } = CspMode.Off;
    public string[] AllowedFrameAncestors { get; set; } = ["'none'"];
    public string[] AdditionalScriptSources { get; set; } = System.Array.Empty<string>();
}
