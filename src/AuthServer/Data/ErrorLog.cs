namespace AuthServer.Data;

public sealed class ErrorLog
{
    public Guid Id { get; set; }

    public DateTime TimestampUtc { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public Guid? ActorUserId { get; set; }

    public string Path { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string? ExceptionType { get; set; }

    public string? StackTrace { get; set; }

    public string? InnerException { get; set; }

    public string? DataJson { get; set; }

    public string? UserAgent { get; set; }

    public string? RemoteIp { get; set; }
}
