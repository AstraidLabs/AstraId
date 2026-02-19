namespace AuthServer.Options;

public sealed class GraphOptions
{
    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string FromUser { get; set; } = string.Empty;
}
