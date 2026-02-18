namespace AppServer.Modules.Game.Application;

public class GameOptions
{
    public const string SectionName = "Game";
    public int TickCapMinutes { get; set; } = 240;
    public int ShieldHoursOnGraduation { get; set; } = 48;
}
