namespace AuthServer.Services.Admin.Models;

public sealed record AdminErrorLogListItem(
    Guid Id,
    DateTime TimestampUtc,
    string TraceId,
    string Path,
    string Method,
    int StatusCode,
    string Title,
    string Detail,
    Guid? ActorUserId,
    string? ActorEmail);
