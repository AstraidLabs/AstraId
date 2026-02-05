namespace AuthServer.Services.Governance;

public sealed class GovernancePolicyInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GovernancePolicyInitializer> _logger;

    public GovernancePolicyInitializer(IServiceProvider serviceProvider, ILogger<GovernancePolicyInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<GovernancePolicyStore>();
        await store.EnsureDefaultsAsync(cancellationToken);
        _logger.LogInformation("Governance policies initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
