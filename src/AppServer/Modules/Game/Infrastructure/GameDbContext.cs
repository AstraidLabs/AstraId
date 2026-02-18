using AppServer.Modules.Game.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Modules.Game.Infrastructure;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<GamePlayer> Players => Set<GamePlayer>();
    public DbSet<GameSharedGalaxy> SharedGalaxies => Set<GameSharedGalaxy>();
    public DbSet<GameSystemState> SystemStates => Set<GameSystemState>();
    public DbSet<GamePlanetState> PlanetStates => Set<GamePlanetState>();
    public DbSet<GameResourceState> ResourceStates => Set<GameResourceState>();
    public DbSet<GameResearchState> ResearchStates => Set<GameResearchState>();
    public DbSet<GameCommand> Commands => Set<GameCommand>();
    public DbSet<GameEventState> EventStates => Set<GameEventState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GamePlayer>(entity =>
        {
            entity.ToTable("GamePlayers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserSub).IsUnique();
            entity.Property(x => x.UserSub).HasMaxLength(200);
            entity.Property(x => x.PrivateSeed).HasMaxLength(100);
            entity.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<GameSharedGalaxy>(entity =>
        {
            entity.ToTable("GameSharedGalaxies");
            entity.HasKey(x => x.ShardId);
            entity.Property(x => x.Seed).HasMaxLength(100);
        });

        modelBuilder.Entity<GameSystemState>(entity =>
        {
            entity.ToTable("GameSystemStates");
            entity.HasKey(x => new { x.PlayerId, x.SystemId });
            entity.HasIndex(x => new { x.PlayerId, x.SystemId }).IsUnique();
            entity.Property(x => x.SystemId).HasMaxLength(80);
            entity.Property(x => x.SurveyProgress).HasPrecision(4, 3);
        });

        modelBuilder.Entity<GamePlanetState>(entity =>
        {
            entity.ToTable("GamePlanetStates");
            entity.HasKey(x => new { x.PlayerId, x.SystemId, x.PlanetIndex });
            entity.Property(x => x.Pop).HasPrecision(12, 4);
        });

        modelBuilder.Entity<GameResourceState>(entity =>
        {
            entity.ToTable("GameResourceStates");
            entity.HasKey(x => x.PlayerId);
            entity.Property(x => x.Energy).HasPrecision(14, 4);
            entity.Property(x => x.Minerals).HasPrecision(14, 4);
            entity.Property(x => x.Alloys).HasPrecision(14, 4);
            entity.Property(x => x.Research).HasPrecision(14, 4);
            entity.Property(x => x.Influence).HasPrecision(14, 4);
            entity.Property(x => x.Unity).HasPrecision(14, 4);
        });

        modelBuilder.Entity<GameResearchState>(entity => { entity.ToTable("GameResearchStates"); entity.HasKey(x => x.PlayerId); });
        modelBuilder.Entity<GameCommand>(entity =>
        {
            entity.ToTable("GameCommands");
            entity.HasKey(x => x.CommandId);
            entity.HasIndex(x => x.CommandId).IsUnique();
            entity.HasIndex(x => new { x.PlayerId, x.CreatedUtc });
            entity.Property(x => x.Type).HasMaxLength(120);
        });

        modelBuilder.Entity<GameEventState>(entity => { entity.ToTable("GameEventStates"); entity.HasKey(x => x.PlayerId); });
    }
}
