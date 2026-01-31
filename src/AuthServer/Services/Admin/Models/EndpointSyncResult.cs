namespace AuthServer.Services.Admin.Models;

public sealed record EndpointSyncResult(int CreatedCount, int UpdatedCount, int DeactivatedCount);
