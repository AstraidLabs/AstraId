namespace AuthServer.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    public bool AllowCredentials { get; set; } = true;
}
