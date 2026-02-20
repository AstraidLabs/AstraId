namespace Api.Options;

/// <summary>
/// Provides configuration options for service client.
/// </summary>
public sealed class ServiceClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string HealthCheckPath { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
