namespace AuthServer.Services.Admin.Models;

public sealed record AdminAuditListItem(
    Guid Id,
    DateTime TimestampUtc,
    string Action,
    string TargetType,
    string? TargetId,
    Guid? ActorUserId,
    string? ActorEmail,
    string? DataJson);
