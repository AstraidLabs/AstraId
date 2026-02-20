namespace Api.Options;

/// <summary>
/// Provides configuration options for ops endpoints.
/// </summary>
public sealed class OpsEndpointsOptions
{
    public const string SectionName = "OpsEndpoints";

    public bool Enabled { get; set; } = true;

    public int CheckIntervalSeconds { get; set; } = 30;

    public string[] CriticalChecks { get; set; } = [];
}
