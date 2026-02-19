namespace AuthServer.Options;

public sealed class SeededClientSecretsOptions
{
    public const string SectionName = "AuthServer:SeededClientSecrets";

    public Dictionary<string, string> SecretsByClientId { get; set; } = new(StringComparer.Ordinal);
}
