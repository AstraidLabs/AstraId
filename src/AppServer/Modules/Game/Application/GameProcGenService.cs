using System.Globalization;
using AppServer.Modules.Game.Contracts;
using AppServer.Modules.Game.Domain;

namespace AppServer.Modules.Game.Application;

public interface IGameProcGenService
{
    GameSystemDto GeneratePrivateStarterSystem(GamePlayer player);
    GalaxyViewDto GenerateSharedGalaxy(GamePlayer player, string seed, int version, bool includeUnknown = false);
}

public class GameProcGenService : IGameProcGenService
{
    public GameSystemDto GeneratePrivateStarterSystem(GamePlayer player)
    {
        var rng = new Xoshiro256StarStar(ParseSeed(player.PrivateSeed));
        var count = rng.NextInt(6, 13);
        var habitableForced = new HashSet<int> { 1, 3 };
        var planets = new List<GamePlanetDto>(count);

        for (var i = 0; i < count; i++)
        {
            var habitability = habitableForced.Contains(i) ? 0.72m + (decimal)rng.NextDouble() * 0.2m : (decimal)rng.NextDouble() * 0.65m;
            var temp = rng.NextDouble();
            var moisture = rng.NextDouble();
            var radiation = rng.NextDouble();
            var biomes = DeriveBiomes(temp, moisture, radiation);
            planets.Add(new GamePlanetDto(i, $"P-{i + 1}", false, 0, biomes, decimal.Round(habitability, 3)));
        }

        return new GameSystemDto("starter-1", "Helios Cradle", "G", true, 0, planets.ToArray());
    }

    public GalaxyViewDto GenerateSharedGalaxy(GamePlayer player, string seed, int version, bool includeUnknown = false)
    {
        var rng = new Xoshiro256StarStar(ParseSeed(seed) + (ulong)version);
        var nodes = new List<GalaxyNodeDto>();
        var edges = new List<GalaxyEdgeDto>();
        var systemCount = 120;

        for (var i = 0; i < systemCount; i++)
        {
            var arm = i % 4;
            var radius = (float)Math.Sqrt(rng.NextDouble()) * 850f;
            var angle = (float)(radius / 180f + (arm * Math.PI / 2) + rng.NextDouble() * 0.2);
            var x = radius * MathF.Cos(angle);
            var y = radius * MathF.Sin(angle);
            var starClass = i % 49 == 0 ? "BlackHoleQuiet" : i % 61 == 0 ? "Neutron" : "MainSequence";
            var known = player.Phase == GamePlayerPhase.Galactic ? includeUnknown || rng.NextDouble() > 0.3 : i == 0;
            nodes.Add(new GalaxyNodeDto($"s-{i}", x, y, starClass, known, i == 0));
        }

        for (var i = 1; i < systemCount; i++)
        {
            edges.Add(new GalaxyEdgeDto($"s-{i - 1}", $"s-{i}"));
            if (i % 5 == 0) edges.Add(new GalaxyEdgeDto($"s-{Math.Max(0, i - 5)}", $"s-{i}"));
        }

        var validatedNodes = nodes
            .Where(n => n.StarClass != "BlackHoleQuiet" || Math.Abs(n.X) > 200 || Math.Abs(n.Y) > 200)
            .ToArray();

        return new GalaxyViewDto(player.Phase.ToString(), validatedNodes, edges.ToArray(), ["core", "fringe", "nebula", "molecular-cloud"]);
    }

    private static ulong ParseSeed(string seed)
    {
        if (ulong.TryParse(seed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return (ulong)seed.Aggregate(1469598103934665603L, (a, c) => unchecked((a ^ c) * 1099511628211L));
    }

    private static string[] DeriveBiomes(double temp, double moisture, double radiation)
    {
        var tags = new List<string>();
        tags.Add(temp > 0.75 ? "Arid" : temp < 0.3 ? "Glacial" : "Temperate");
        tags.Add(moisture > 0.7 ? "Oceanic" : moisture < 0.25 ? "Dry" : "Continental");
        if (radiation > 0.8) tags.Add("Irradiated");
        if (temp > 0.6 && moisture > 0.6) tags.Add("Jungle");
        return tags.ToArray();
    }
}
