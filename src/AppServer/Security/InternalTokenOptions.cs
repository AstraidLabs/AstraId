namespace AppServer.Security;

public sealed class InternalTokenOptions
{
    public const string SectionName = "InternalTokens";

    public string Issuer { get; set; } = "astraid-api";
    public string Audience { get; set; } = "astraid-app";
    public string SigningKey { get; set; } = string.Empty;
}
