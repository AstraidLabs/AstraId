namespace Api.Models;

public sealed record PolicyMapEntry(
    string Method,
    string Path,
    IReadOnlyCollection<string> RequiredPermissions);
