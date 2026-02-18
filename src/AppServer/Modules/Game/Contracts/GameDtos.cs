namespace AppServer.Modules.Game.Contracts;

public record GamePlayerDto(Guid PlayerId, string UserSub, string Phase, DateTimeOffset? ShieldUntilUtc);

public record GameResourceDto(decimal Energy, decimal Minerals, decimal Alloys, decimal Research, decimal Influence, decimal Unity);

public record GamePlanetDto(int PlanetIndex, string Name, bool Colonized, decimal Pop, string[] Biomes, decimal Habitability);

public record GameSystemDto(string SystemId, string Name, string StarType, bool Owned, decimal SurveyProgress, GamePlanetDto[] Planets);

public record GalaxyNodeDto(string SystemId, float X, float Y, string StarClass, bool Known, bool Owned);

public record GalaxyEdgeDto(string A, string B);

public record GalaxyViewDto(string Phase, GalaxyNodeDto[] Nodes, GalaxyEdgeDto[] Edges, string[] Regions);

public record GameStateDto(GamePlayerDto Player, GameResourceDto Resources, string? ActiveResearchProjectId, decimal ResearchProgress, bool GraduationReady);

public record GameCommandRequest(Guid CommandId, string Type, Dictionary<string, object?> Payload);

public record GameCommandResponse(Guid CommandId, string Status, string Message, GameStateDto State);
