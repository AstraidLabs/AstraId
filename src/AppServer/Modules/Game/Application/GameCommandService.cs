using System.Text.Json;
using AppServer.Modules.Game.Contracts;
using AppServer.Modules.Game.Domain;
using AppServer.Modules.Game.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppServer.Modules.Game.Application;

public interface IGameCommandService
{
    Task<GameCommandResponse> ExecuteAsync(GamePlayer player, GameCommandRequest request, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}

public class GameCommandRequestValidator : AbstractValidator<GameCommandRequest>
{
    public static readonly HashSet<string> AllowedTypes = ["StartSurvey", "BuildOutpost", "ColonizePlanet", "StartResearch", "SetPolicy"];

    public GameCommandRequestValidator()
    {
        RuleFor(x => x.CommandId).NotEmpty();
        RuleFor(x => x.Type).Must(AllowedTypes.Contains).WithMessage("Unsupported command type.");
        RuleFor(x => x.Payload).NotNull();
    }
}

public class GameCommandService(
    GameDbContext dbContext,
    IValidator<GameCommandRequest> validator,
    IGameStateService gameStateService,
    IOptions<GameOptions> options,
    ILogger<GameCommandService> logger) : IGameCommandService
{
    public async Task<GameCommandResponse> ExecuteAsync(GamePlayer player, GameCommandRequest request, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existing = await dbContext.Commands.FindAsync([request.CommandId], cancellationToken);
        if (existing is not null)
        {
            var state = await gameStateService.BuildStateAsync(player, nowUtc, cancellationToken);
            return new GameCommandResponse(request.CommandId, existing.Status.ToString(), "Duplicate command ignored.", state);
        }

        var command = new GameCommand
        {
            CommandId = request.CommandId,
            PlayerId = player.Id,
            Type = request.Type,
            PayloadJson = JsonSerializer.Serialize(request.Payload),
            CreatedUtc = nowUtc,
            Status = GameCommandStatus.Pending
        };

        dbContext.Commands.Add(command);

        try
        {
            await ApplyCommandAsync(player, request, cancellationToken);
            command.Status = GameCommandStatus.Applied;
            command.ResultJson = "{\"ok\":true}";
            command.ProcessedUtc = nowUtc;

            await EvaluateGraduationAsync(player, nowUtc, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            var state = await gameStateService.BuildStateAsync(player, nowUtc, cancellationToken);
            logger.LogInformation("Applied game command {CommandType} for player {PlayerId}", request.Type, player.Id);
            return new GameCommandResponse(request.CommandId, command.Status.ToString(), "Applied", state);
        }
        catch (Exception ex)
        {
            command.Status = GameCommandStatus.Rejected;
            command.ResultJson = JsonSerializer.Serialize(new { error = ex.Message });
            command.ProcessedUtc = nowUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogWarning(ex, "Rejected command {CommandType} for player {PlayerId}", request.Type, player.Id);
            var state = await gameStateService.BuildStateAsync(player, nowUtc, cancellationToken);
            return new GameCommandResponse(request.CommandId, command.Status.ToString(), ex.Message, state);
        }
    }

    private async Task ApplyCommandAsync(GamePlayer player, GameCommandRequest request, CancellationToken cancellationToken)
    {
        switch (request.Type)
        {
            case "StartSurvey":
                var surveySystemId = RequireString(request.Payload, "systemId");
                var targetSystem = await dbContext.SystemStates.FindAsync([player.Id, surveySystemId], cancellationToken)
                    ?? throw new InvalidOperationException("System not known.");
                targetSystem.DiscoveryState = GameDiscoveryState.Surveying;
                break;
            case "BuildOutpost":
                var outpostSystemId = RequireString(request.Payload, "systemId");
                var outpostSystem = await dbContext.SystemStates.FindAsync([player.Id, outpostSystemId], cancellationToken)
                    ?? throw new InvalidOperationException("System not known.");
                outpostSystem.Owned = true;
                break;
            case "ColonizePlanet":
                var colonizeSystemId = RequireString(request.Payload, "systemId");
                var planetIndex = RequireInt(request.Payload, "planetIndex");
                var planet = await dbContext.PlanetStates.FindAsync([player.Id, colonizeSystemId, planetIndex], cancellationToken)
                    ?? throw new InvalidOperationException("Planet not found.");
                planet.Colonized = true;
                if (planet.Pop <= 0) planet.Pop = 1;
                break;
            case "StartResearch":
                var projectId = RequireString(request.Payload, "projectId");
                var research = await dbContext.ResearchStates.FindAsync([player.Id], cancellationToken)
                    ?? throw new InvalidOperationException("Research state missing.");
                research.ActiveProjectId = projectId;
                research.Progress = 0;
                break;
            case "SetPolicy":
                break;
            default:
                throw new InvalidOperationException("Unknown command type.");
        }
    }

    private async Task EvaluateGraduationAsync(GamePlayer player, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        if (player.Phase != GamePlayerPhase.ProtectedSystem) return;

        var surveyed = await dbContext.SystemStates.Where(x => x.PlayerId == player.Id).AverageAsync(x => (double)x.SurveyProgress, cancellationToken);
        var colonizedCount = await dbContext.PlanetStates.CountAsync(x => x.PlayerId == player.Id && x.Colonized, cancellationToken);
        var research = await dbContext.ResearchStates.SingleAsync(x => x.PlayerId == player.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(research.ActiveProjectId)) return;
        var completed = JsonSerializer.Deserialize<List<string>>(research.CompletedJson) ?? [];

        if (!completed.Any(x => x.Contains("ftl", StringComparison.OrdinalIgnoreCase)) || colonizedCount < 2 || surveyed < 0.75)
        {
            return;
        }

        var shard = await dbContext.SharedGalaxies.OrderBy(x => x.CreatedUtc).FirstOrDefaultAsync(cancellationToken);
        if (shard is null)
        {
            shard = new GameSharedGalaxy
            {
                ShardId = Guid.NewGuid(),
                Seed = "424242",
                GenerationVersion = 1,
                ParamsJson = "{\"shape\":\"spiral\"}",
                CreatedUtc = nowUtc
            };
            dbContext.SharedGalaxies.Add(shard);
        }

        player.Phase = GamePlayerPhase.Galactic;
        player.SharedShardId = shard.ShardId;
        player.ShieldUntilUtc = nowUtc.AddHours(options.Value.ShieldHoursOnGraduation);

        var exists = await dbContext.SystemStates.FindAsync([player.Id, "s-0"], cancellationToken);
        if (exists is null)
        {
            dbContext.SystemStates.Add(new GameSystemState
            {
                PlayerId = player.Id,
                SystemId = "s-0",
                DiscoveryState = GameDiscoveryState.Known,
                Owned = true,
                SurveyProgress = 0
            });
        }
    }

    private static string RequireString(IReadOnlyDictionary<string, object?> payload, string key)
    {
        if (!payload.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value?.ToString()))
            throw new InvalidOperationException($"{key} is required.");
        return value.ToString()!;
    }

    private static int RequireInt(IReadOnlyDictionary<string, object?> payload, string key)
    {
        if (!payload.TryGetValue(key, out var value) || !int.TryParse(value?.ToString(), out var parsed))
            throw new InvalidOperationException($"{key} must be an integer.");
        return parsed;
    }
}
