namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for send grid.
/// </summary>
public sealed class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;
}
