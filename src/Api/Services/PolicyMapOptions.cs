namespace Api.Services;

public sealed class PolicyMapOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int RefreshMinutes { get; set; } = 5;
}
