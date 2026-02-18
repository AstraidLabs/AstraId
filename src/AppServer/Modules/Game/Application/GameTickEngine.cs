using System.Text.Json;
using AppServer.Modules.Game.Domain;
using AppServer.Modules.Game.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppServer.Modules.Game.Application;

public interface IGameTickEngine
{
    Task ApplyTickAsync(GamePlayer player, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}

public class GameTickEngine(GameDbContext dbContext, IOptions<GameOptions> options, ILogger<GameTickEngine> logger) : IGameTickEngine
{
    public async Task ApplyTickAsync(GamePlayer player, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        if (nowUtc < player.LastTickUtc)
        {
            logger.LogWarning("Ignoring negative tick delta for player {PlayerId}", player.Id);
            return;
        }

        var maxDelta = TimeSpan.FromMinutes(options.Value.TickCapMinutes);
        var delta = nowUtc - player.LastTickUtc;
        if (delta > maxDelta)
        {
            delta = maxDelta;
        }

        var minutes = (decimal)delta.TotalMinutes;
        if (minutes <= 0) return;

        var resources = await dbContext.ResourceStates.SingleAsync(x => x.PlayerId == player.Id, cancellationToken);
        resources.Energy += 12m * minutes;
        resources.Minerals += 8m * minutes;
        resources.Alloys += 1.5m * minutes;
        resources.Research += 4m * minutes;
        resources.Influence += 0.2m * minutes;
        resources.Unity += 0.4m * minutes;

        var research = await dbContext.ResearchStates.SingleAsync(x => x.PlayerId == player.Id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(research.ActiveProjectId))
        {
            research.Progress += 2m * minutes;
            if (research.Progress >= 1000m)
            {
                var completed = JsonSerializer.Deserialize<List<string>>(research.CompletedJson) ?? [];
                completed.Add(research.ActiveProjectId);
                research.CompletedJson = JsonSerializer.Serialize(completed.Distinct());
                research.ActiveProjectId = null;
                research.Progress = 0;
            }
        }

        foreach (var planet in dbContext.PlanetStates.Where(x => x.PlayerId == player.Id && x.Colonized))
        {
            planet.Pop += minutes * 0.03m;
        }

        foreach (var system in dbContext.SystemStates.Where(x => x.PlayerId == player.Id && x.DiscoveryState == GameDiscoveryState.Surveying))
        {
            system.SurveyProgress = Math.Min(1m, system.SurveyProgress + (minutes / 300m));
            if (system.SurveyProgress >= 1m)
            {
                system.DiscoveryState = GameDiscoveryState.Surveyed;
            }
        }

        player.LastTickUtc = nowUtc;
        player.UpdatedUtc = nowUtc;
    }
}
