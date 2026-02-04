using AuthServer.Data;
using AuthServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Diagnostics;

public sealed class ErrorLogCleanupService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DiagnosticsOptions> _options;
    private readonly ILogger<ErrorLogCleanupService> _logger;

    public ErrorLogCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<DiagnosticsOptions> options,
        ILogger<ErrorLogCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupAsync(stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (!options.StoreErrorLogs || options.MaxStoredDays <= 0)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cutoff = DateTime.UtcNow.AddDays(-options.MaxStoredDays);
            var deleted = await dbContext.ErrorLogs
                .Where(log => log.TimestampUtc < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted > 0)
            {
                _logger.LogInformation("Deleted {Deleted} error logs older than {Cutoff}.", deleted, cutoff);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup error logs.");
        }
    }
}
