using AppServer.Modules.Game.Application;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Modules.Game.Infrastructure;

public static class GameModuleServiceCollectionExtensions
{
    public static IServiceCollection AddGameModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GameDatabase")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=astraid_game;Username=postgres;Password=postgres";

        services.AddDbContext<GameDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IGameTickEngine, GameTickEngine>();
        services.AddScoped<IGameProcGenService, GameProcGenService>();
        services.AddScoped<IGameStateService, GameStateService>();
        services.AddScoped<IGameCommandService, GameCommandService>();
        services.AddScoped<IValidator<Contracts.GameCommandRequest>, GameCommandRequestValidator>();

        services.AddOptions<GameOptions>()
            .Bind(configuration.GetSection(GameOptions.SectionName))
            .ValidateDataAnnotations();

        return services;
    }
}
