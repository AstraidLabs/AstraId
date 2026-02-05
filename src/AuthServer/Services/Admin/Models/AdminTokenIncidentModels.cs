namespace AuthServer.Services.Admin.Models;

public sealed record AdminTokenIncidentListItem(
    Guid Id,
    DateTime TimestampUtc,
    string Type,
    string Severity,
    Guid? UserId,
    string? ClientId,
    string? TraceId,
    string? DetailJson);

public sealed record AdminTokenIncidentDetail(
    Guid Id,
    DateTime TimestampUtc,
    string Type,
    string Severity,
    Guid? UserId,
    string? ClientId,
    string? TraceId,
    string? DetailJson,
    Guid? ActorUserId);
