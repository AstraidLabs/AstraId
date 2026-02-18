using AppServer.Modules.Game.Application;
using AppServer.Modules.Game.Contracts;
using AppServer.Security;

namespace AppServer.Modules.Game.Api;

public static class GameEndpoints
{
    public static RouteGroupBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/game").RequireAuthorization();

        group.MapGet("/me", async (ICurrentUser currentUser, IGameStateService stateService, CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var player = await stateService.GetOrCreatePlayerAsync(currentUser.Subject, now, cancellationToken);
            return Results.Ok(new GamePlayerDto(player.Id, player.UserSub, player.Phase.ToString(), player.ShieldUntilUtc));
        });

        group.MapGet("/state", async (ICurrentUser currentUser, IGameStateService stateService, CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var player = await stateService.GetOrCreatePlayerAsync(currentUser.Subject, now, cancellationToken);
            var state = await stateService.BuildStateAsync(player, now, cancellationToken);
            return Results.Ok(state);
        }).RequireRateLimiting("game-state");

        group.MapGet("/galaxy", async (ICurrentUser currentUser, IGameStateService stateService, CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var player = await stateService.GetOrCreatePlayerAsync(currentUser.Subject, now, cancellationToken);
            var galaxy = await stateService.BuildGalaxyViewAsync(player, cancellationToken);
            return Results.Ok(galaxy);
        });

        group.MapGet("/system/{id}", async (string id, ICurrentUser currentUser, IGameStateService stateService, CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var player = await stateService.GetOrCreatePlayerAsync(currentUser.Subject, now, cancellationToken);
            var system = await stateService.GetSystemAsync(player, id, cancellationToken);
            return system is null ? Results.NotFound() : Results.Ok(system);
        });

        group.MapPost("/commands", async (GameCommandRequest request, ICurrentUser currentUser, IGameStateService stateService, IGameCommandService commandService, CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var player = await stateService.GetOrCreatePlayerAsync(currentUser.Subject, now, cancellationToken);
            var result = await commandService.ExecuteAsync(player, request, now, cancellationToken);
            return Results.Ok(result);
        }).RequireRateLimiting("game-commands");

        return group;
    }
}
