namespace AppServer.Modules.Game.Domain;

public enum GamePlayerPhase
{
    ProtectedSystem = 0,
    Galactic = 1
}

public enum GameDiscoveryState
{
    Unknown = 0,
    Known = 1,
    Surveying = 2,
    Surveyed = 3
}

public enum GameCommandStatus
{
    Pending = 0,
    Applied = 1,
    Rejected = 2
}
