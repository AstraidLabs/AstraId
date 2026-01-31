namespace AuthServer.Models;

public sealed record ApiPolicyMapEntryDto(
    string Method,
    string Path,
    IReadOnlyCollection<string> RequiredPermissions);
