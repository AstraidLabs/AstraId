using Microsoft.EntityFrameworkCore;

namespace AppServer.Modules.Game.Domain;

public class GamePlayer
{
    public Guid Id { get; set; }
    public string UserSub { get; set; } = string.Empty;
    public GamePlayerPhase Phase { get; set; } = GamePlayerPhase.ProtectedSystem;
    public string PrivateSeed { get; set; } = string.Empty;
    public Guid? SharedShardId { get; set; }
    public DateTimeOffset LastTickUtc { get; set; }
    public DateTimeOffset? ShieldUntilUtc { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public uint RowVersion { get; set; }
}

public class GameSharedGalaxy
{
    public Guid ShardId { get; set; }
    public string Seed { get; set; } = string.Empty;
    public int GenerationVersion { get; set; } = 1;
    public string ParamsJson { get; set; } = "{}";
    public DateTimeOffset CreatedUtc { get; set; }
}

public class GameSystemState
{
    public Guid PlayerId { get; set; }
    public string SystemId { get; set; } = string.Empty;
    public GameDiscoveryState DiscoveryState { get; set; }
    public bool Owned { get; set; }
    public decimal SurveyProgress { get; set; }
}

public class GamePlanetState
{
    public Guid PlayerId { get; set; }
    public string SystemId { get; set; } = string.Empty;
    public int PlanetIndex { get; set; }
    public bool Colonized { get; set; }
    public decimal Pop { get; set; }
    public string BuildingsJson { get; set; } = "[]";
}

public class GameResourceState
{
    public Guid PlayerId { get; set; }
    public decimal Energy { get; set; }
    public decimal Minerals { get; set; }
    public decimal Alloys { get; set; }
    public decimal Research { get; set; }
    public decimal Influence { get; set; }
    public decimal Unity { get; set; }
    public string StorageCapsJson { get; set; } = "{}";
}

public class GameResearchState
{
    public Guid PlayerId { get; set; }
    public string? ActiveProjectId { get; set; }
    public decimal Progress { get; set; }
    public string CompletedJson { get; set; } = "[]";
}

public class GameCommand
{
    public Guid CommandId { get; set; }
    public Guid PlayerId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ProcessedUtc { get; set; }
    public GameCommandStatus Status { get; set; } = GameCommandStatus.Pending;
    public string ResultJson { get; set; } = "{}";
}

public class GameEventState
{
    public Guid PlayerId { get; set; }
    public string ActiveEventsJson { get; set; } = "[]";
    public string PendingEventsJson { get; set; } = "[]";
}
