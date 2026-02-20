namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for postmark.
/// </summary>
public sealed class PostmarkOptions
{
    public string ServerToken { get; set; } = string.Empty;
}
