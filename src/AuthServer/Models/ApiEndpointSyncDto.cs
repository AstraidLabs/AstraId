namespace AuthServer.Models;

public sealed record ApiEndpointSyncDto(
    string Method,
    string Path,
    string? DisplayName,
    bool? Deprecated,
    string? Tags);
