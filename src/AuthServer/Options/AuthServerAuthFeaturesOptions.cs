namespace AuthServer.Options;

public sealed class AuthServerAuthFeaturesOptions
{
    public const string SectionName = "AuthServer:Features";

    public bool EnablePasswordGrant { get; set; } = false;
}
