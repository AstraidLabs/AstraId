namespace AuthServer.Services.Admin.Models;

public sealed record AdminApiResourceRequest(
    string Name,
    string DisplayName,
    string? BaseUrl,
    bool IsActive);
