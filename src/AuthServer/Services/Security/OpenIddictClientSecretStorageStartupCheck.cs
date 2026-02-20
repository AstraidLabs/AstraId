using AstraId.Logging.Audit;

namespace AuthServer.Services.Security;

/// <summary>
/// Provides open iddict client secret storage startup check functionality.
/// </summary>
public sealed class OpenIddictClientSecretStorageStartupCheck : IHostedService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OpenIddictClientSecretStorageStartupCheck> _logger;
    private readonly ISecurityAuditLogger _auditLogger;

    public OpenIddictClientSecretStorageStartupCheck(
        IWebHostEnvironment environment,
        IServiceProvider serviceProvider,
        ILogger<OpenIddictClientSecretStorageStartupCheck> logger,
        ISecurityAuditLogger auditLogger)
    {
        _environment = environment;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _auditLogger = auditLogger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return;
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var inspector = scope.ServiceProvider.GetRequiredService<IOpenIddictClientSecretInspector>();
        var (total, looksHashed, looksPlaintext) = await inspector.InspectAsync(cancellationToken);

        _logger.LogInformation("OpenIddict confidential client secret storage check completed. Total={Total}, LooksHashed={LooksHashed}, LooksPlaintext={LooksPlaintext}", total, looksHashed, looksPlaintext);

        if (looksPlaintext > 0)
        {
            _auditLogger.Log(new SecurityAuditEvent
            {
                EventType = "auth.openiddict_client_secret_storage.warning",
                Service = "AuthServer",
                Environment = _environment.EnvironmentName,
                ActorType = "system",
                Target = "OpenIddictApplications",
                Action = "startup_secret_storage_check",
                Result = "warning",
                ReasonCode = "client_secret_looks_plaintext"
            });
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
