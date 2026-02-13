namespace Api.Options;

public sealed class InternalTokenOptions
{
    public const string SectionName = "InternalTokens";

    public string Issuer { get; set; } = "astraid-api";
    public string Audience { get; set; } = "astraid-content";
    public int LifetimeMinutes { get; set; } = 2;
    public string SigningKey { get; set; } = string.Empty;
}
