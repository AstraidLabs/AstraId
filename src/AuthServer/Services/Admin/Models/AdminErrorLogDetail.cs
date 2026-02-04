namespace AuthServer.Services.Admin.Models;

public sealed record AdminErrorLogDetail(
    Guid Id,
    DateTime TimestampUtc,
    string TraceId,
    string Path,
    string Method,
    int StatusCode,
    string Title,
    string Detail,
    string? ExceptionType,
    string? StackTrace,
    string? InnerException,
    string? DataJson,
    Guid? ActorUserId,
    string? ActorEmail,
    string? UserAgent,
    string? RemoteIp);
