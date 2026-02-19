namespace Api.Contracts.Ops;

public sealed class PlatformHealthDto
{
    public required string OverallStatus { get; init; }
    public required DateTimeOffset CheckedAtUtc { get; init; }
    public required int NextCheckInSeconds { get; init; }
    public required string Environment { get; init; }
    public required string Service { get; init; }
    public string? Version { get; init; }
    public required PlatformHealthSummaryDto Summary { get; init; }
    public required IReadOnlyList<PlatformHealthCheckDto> Checks { get; init; }
}

public sealed class PlatformHealthSummaryDto
{
    public required int Healthy { get; init; }
    public required int Degraded { get; init; }
    public required int Unhealthy { get; init; }
}

public sealed class PlatformHealthCheckDto
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
    public required bool IsCritical { get; init; }
    public required long DurationMs { get; init; }
    public DateTimeOffset? LastSuccessUtc { get; init; }
    public string? Message { get; init; }
}
