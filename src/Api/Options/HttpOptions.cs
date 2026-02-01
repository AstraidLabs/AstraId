namespace Api.Options;

public sealed class HttpOptions
{
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public double RetryBaseDelaySeconds { get; set; } = 1;
}
