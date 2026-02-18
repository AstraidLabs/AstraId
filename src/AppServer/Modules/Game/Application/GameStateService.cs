using System.Text.Json;
using AppServer.Modules.Game.Contracts;
using AppServer.Modules.Game.Domain;
using AppServer.Modules.Game.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AppServer.Modules.Game.Application;

public interface IGameStateService
{
    Task<GamePlayer> GetOrCreatePlayerAsync(string userSub, DateTimeOffset nowUtc, CancellationToken cancellationToken);
    Task<GameStateDto> BuildStateAsync(GamePlayer player, DateTimeOffset nowUtc, CancellationToken cancellationToken);
    Task<GalaxyViewDto> BuildGalaxyViewAsync(GamePlayer player, CancellationToken cancellationToken);
    Task<GameSystemDto?> GetSystemAsync(GamePlayer player, string systemId, CancellationToken cancellationToken);
}

public class GameStateService(GameDbContext dbContext, IGameProcGenService procGenService, IGameTickEngine tickEngine) : IGameStateService
{
    public async Task<GamePlayer> GetOrCreatePlayerAsync(string userSub, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var player = await dbContext.Players.SingleOrDefaultAsync(x => x.UserSub == userSub, cancellationToken);
        if (player is not null) return player;

        var candidate = new GamePlayer
        {
            Id = Guid.NewGuid(),
            UserSub = userSub,
            PrivateSeed = HashSeed(userSub),
            LastTickUtc = nowUtc,
            CreatedUtc = nowUtc,
            UpdatedUtc = nowUtc
        };

        var starter = procGenService.GeneratePrivateStarterSystem(candidate);

        dbContext.Players.Add(candidate);
        dbContext.ResourceStates.Add(new GameResourceState
        {
            PlayerId = candidate.Id,
            Energy = 100,
            Minerals = 100,
            Alloys = 25,
            Research = 0,
            Influence = 10,
            Unity = 0,
            StorageCapsJson = JsonSerializer.Serialize(new { energy = 2000, minerals = 2000, alloys = 800, research = 2000 })
        });
        dbContext.ResearchStates.Add(new GameResearchState { PlayerId = candidate.Id, ActiveProjectId = "ftl-theory", Progress = 0, CompletedJson = "[]" });
        dbContext.EventStates.Add(new GameEventState { PlayerId = candidate.Id, ActiveEventsJson = "[]", PendingEventsJson = "[]" });
        dbContext.SystemStates.Add(new GameSystemState
        {
            PlayerId = candidate.Id,
            SystemId = starter.SystemId,
            DiscoveryState = GameDiscoveryState.Known,
            Owned = true,
            SurveyProgress = 0
        });

        foreach (var planet in starter.Planets)
        {
            dbContext.PlanetStates.Add(new GamePlanetState
            {
                PlayerId = candidate.Id,
                SystemId = starter.SystemId,
                PlanetIndex = planet.PlanetIndex,
                Colonized = false,
                Pop = 0,
                BuildingsJson = "[]"
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return candidate;
        }
        catch (DbUpdateException ex) when (IsDuplicateUserSubViolation(ex))
        {
            dbContext.ChangeTracker.Clear();

            var existing = await dbContext.Players.SingleAsync(x => x.UserSub == userSub, cancellationToken);
            return existing;
        }
    }

    private static bool IsDuplicateUserSubViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && string.Equals(postgres.ConstraintName, "IX_GamePlayers_UserSub", StringComparison.Ordinal);

    public async Task<GameStateDto> BuildStateAsync(GamePlayer player, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        await tickEngine.ApplyTickAsync(player, nowUtc, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var resources = await dbContext.ResourceStates.SingleAsync(x => x.PlayerId == player.Id, cancellationToken);
        var research = await dbContext.ResearchStates.SingleAsync(x => x.PlayerId == player.Id, cancellationToken);

        var completed = JsonSerializer.Deserialize<List<string>>(research.CompletedJson) ?? [];
        var surveyedAvg = await dbContext.SystemStates.Where(x => x.PlayerId == player.Id).AverageAsync(x => (double)x.SurveyProgress, cancellationToken);
        var colonizedCount = await dbContext.PlanetStates.CountAsync(x => x.PlayerId == player.Id && x.Colonized, cancellationToken);

        var graduationReady = completed.Any(x => x.Contains("ftl", StringComparison.OrdinalIgnoreCase)) && surveyedAvg >= 0.75 && colonizedCount >= 2;

        return new GameStateDto(
            new GamePlayerDto(player.Id, player.UserSub, player.Phase.ToString(), player.ShieldUntilUtc),
            new GameResourceDto(resources.Energy, resources.Minerals, resources.Alloys, resources.Research, resources.Influence, resources.Unity),
            research.ActiveProjectId,
            research.Progress,
            graduationReady);
    }

    public async Task<GalaxyViewDto> BuildGalaxyViewAsync(GamePlayer player, CancellationToken cancellationToken)
    {
        if (player.Phase == GamePlayerPhase.ProtectedSystem)
        {
            return new GalaxyViewDto(player.Phase.ToString(), [new GalaxyNodeDto("starter-1", 0, 0, "G", true, true)], [], ["starter"]);
        }

        var shard = await dbContext.SharedGalaxies.SingleAsync(x => x.ShardId == player.SharedShardId, cancellationToken);
        var knownSystems = await dbContext.SystemStates.Where(x => x.PlayerId == player.Id).Select(x => x.SystemId).ToArrayAsync(cancellationToken);
        var full = procGenService.GenerateSharedGalaxy(player, shard.Seed, shard.GenerationVersion, includeUnknown: false);
        var filteredNodes = full.Nodes.Where(n => knownSystems.Contains(n.SystemId) || n.Known).ToArray();
        var filteredSet = filteredNodes.Select(x => x.SystemId).ToHashSet();
        var filteredEdges = full.Edges.Where(e => filteredSet.Contains(e.A) && filteredSet.Contains(e.B)).ToArray();
        return full with { Nodes = filteredNodes, Edges = filteredEdges };
    }

    public async Task<GameSystemDto?> GetSystemAsync(GamePlayer player, string systemId, CancellationToken cancellationToken)
    {
        var known = await dbContext.SystemStates.FindAsync([player.Id, systemId], cancellationToken);
        if (known is null && player.Phase == GamePlayerPhase.ProtectedSystem)
        {
            return null;
        }

        if (systemId == "starter-1")
        {
            var starter = procGenService.GeneratePrivateStarterSystem(player);
            var planets = await dbContext.PlanetStates.Where(x => x.PlayerId == player.Id && x.SystemId == systemId).ToDictionaryAsync(x => x.PlanetIndex, cancellationToken);
            return starter with
            {
                Owned = known?.Owned ?? false,
                SurveyProgress = known?.SurveyProgress ?? 0,
                Planets = starter.Planets.Select(p => planets.TryGetValue(p.PlanetIndex, out var state)
                    ? p with { Colonized = state.Colonized, Pop = state.Pop }
                    : p).ToArray()
            };
        }

        return new GameSystemDto(systemId, systemId.ToUpperInvariant(), "MainSequence", known?.Owned ?? false, known?.SurveyProgress ?? 0,
            [new GamePlanetDto(0, "Frontier", false, 0, ["Rocky"], 0.3m)]);
    }

    private static string HashSeed(string sub) => Math.Abs(sub.GetHashCode()).ToString();
}
