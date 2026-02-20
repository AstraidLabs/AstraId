namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for graph.
/// </summary>
public sealed class GraphOptions
{
    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string FromUser { get; set; } = string.Empty;
}
