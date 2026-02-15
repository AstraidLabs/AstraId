namespace AuthServer.Services.Jobs;

public sealed class SecurityMaintenanceJobs
{
    private readonly ILogger<SecurityMaintenanceJobs> _logger;

    public SecurityMaintenanceJobs(ILogger<SecurityMaintenanceJobs> logger)
    {
        _logger = logger;
    }

    public Task CleanupAsync()
    {
        _logger.LogInformation("Executing scheduled security cleanup task.");
        return Task.CompletedTask;
    }
}
