using AuthServer.Options;
using AuthServer.Services.Governance;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace AuthServer.Services.SigningKeys;

/// <summary>
/// Executes signing key lifecycle for issuer tokens and triggers OpenIddict option refresh after rotation.
/// </summary>
public sealed class SigningKeyRotationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<AuthServerSigningKeyOptions> _options;
    private readonly IOptionsMonitorCache<OpenIddictServerOptions> _optionsCache;
    private readonly ISigningKeyRotationState _state;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SigningKeyRotationService> _logger;

    public SigningKeyRotationService(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AuthServerSigningKeyOptions> options,
        IOptionsMonitorCache<OpenIddictServerOptions> optionsCache,
        ISigningKeyRotationState state,
        IHostEnvironment environment,
        ILogger<SigningKeyRotationService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _optionsCache = optionsCache;
        _state = state;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Runs periodic rotation checks using configured check interval (minutes) and retention policy (days).
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (SigningKeyModeResolver.Resolve(_options.CurrentValue.Mode, _environment) != SigningKeyMode.DbKeyRing)
        {
            _logger.LogInformation("Signing key rotation is disabled because SigningKeys mode is not DbKeyRing.");
            return;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var keyRing = scope.ServiceProvider.GetRequiredService<SigningKeyRingService>();
            await keyRing.EnsureInitializedAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            var mode = SigningKeyModeResolver.Resolve(options.Mode, _environment);

            using var scope = _serviceProvider.CreateScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<SigningKeyRotationCoordinator>();
            var policyService = scope.ServiceProvider.GetRequiredService<KeyRotationPolicyService>();
            var keyRing = scope.ServiceProvider.GetRequiredService<SigningKeyRingService>();

            var policy = await policyService.GetPolicyAsync(stoppingToken);
            if (policy.Enabled && mode == SigningKeyMode.DbKeyRing)
            {
                // Removing OpenIddict cached options forces new signing credentials to be used for newly issued tokens.
                var rotation = await coordinator.RotateIfDueAsync(stoppingToken);
                if (rotation is not null)
                {
                    _optionsCache.TryRemove(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    _state.LastRotationUtc = DateTimeOffset.UtcNow;
                }

                await keyRing.CleanupAsync(policy.GracePeriodDays, stoppingToken);
            }

            var delay = TimeSpan.FromMinutes(Math.Max(1, options.CheckPeriodMinutes));
            _state.NextCheckUtc = DateTimeOffset.UtcNow.Add(delay);
            await Task.Delay(delay, stoppingToken);
        }
    }
}
