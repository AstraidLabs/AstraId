namespace Api.Services;

/// <summary>
/// Provides configuration options for policy map.
/// </summary>
public sealed class PolicyMapOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int RefreshMinutes { get; set; } = 5;
}
