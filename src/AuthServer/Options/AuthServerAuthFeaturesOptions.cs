namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for auth server auth features.
/// </summary>
public sealed class AuthServerAuthFeaturesOptions
{
    public const string SectionName = "AuthServer:Features";

    public bool EnablePasswordGrant { get; set; } = false;
}
