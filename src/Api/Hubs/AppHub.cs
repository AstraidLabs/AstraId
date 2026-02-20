using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;
/// <summary>
/// Provides app hub functionality.
/// </summary>

[Authorize]
public sealed class AppHub : Hub
{
    public Task JoinUserGroup(string userId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

    public Task JoinTenantGroup(string tenantId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
}
