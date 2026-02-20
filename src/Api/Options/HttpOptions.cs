namespace Api.Options;

/// <summary>
/// Provides configuration options for http.
/// </summary>
public sealed class HttpOptions
{
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public double RetryBaseDelaySeconds { get; set; } = 1;
}
