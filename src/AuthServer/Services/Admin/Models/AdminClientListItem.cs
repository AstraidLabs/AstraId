namespace AuthServer.Services.Admin.Models;

public sealed record AdminClientListItem(
    string Id,
    string ClientId,
    string? DisplayName,
    string ClientType,
    bool Enabled);
