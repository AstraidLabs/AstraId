namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for seeded client secrets.
/// </summary>
public sealed class SeededClientSecretsOptions
{
    public const string SectionName = "AuthServer:SeededClientSecrets";

    public Dictionary<string, string> SecretsByClientId { get; set; } = new(StringComparer.Ordinal);
}
