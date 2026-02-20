namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for token exchange.
/// </summary>
public sealed class TokenExchangeOptions
{
    public const string SectionName = "AuthServer:TokenExchange";

    public bool Enabled { get; set; } = false;
    public string[] AllowedClients { get; set; } = [];
    public string[] AllowedAudiences { get; set; } = [];
    public string ActorClaimType { get; set; } = "act";
}
