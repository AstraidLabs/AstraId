namespace AstraId.Contracts;

public sealed record AppEvent(
    string Type,
    string? TenantId,
    string? UserId,
    string EntityId,
    DateTimeOffset OccurredAt,
    object? Payload = null);
