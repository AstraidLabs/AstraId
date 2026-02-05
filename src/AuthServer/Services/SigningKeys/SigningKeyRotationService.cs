using AuthServer.Options;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace AuthServer.Services.SigningKeys;

public sealed class SigningKeyRotationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<AuthServerSigningKeyOptions> _options;
    private readonly IOptionsMonitorCache<OpenIddictServerOptions> _optionsCache;
    private readonly ISigningKeyRotationState _state;
    private readonly ILogger<SigningKeyRotationService> _logger;

    public SigningKeyRotationService(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AuthServerSigningKeyOptions> options,
        IOptionsMonitorCache<OpenIddictServerOptions> optionsCache,
        ISigningKeyRotationState state,
        ILogger<SigningKeyRotationService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _optionsCache = optionsCache;
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var keyRing = scope.ServiceProvider.GetRequiredService<SigningKeyRingService>();
            await keyRing.EnsureInitializedAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            if (options.Enabled)
            {
                using var scope = _serviceProvider.CreateScope();
                var keyRing = scope.ServiceProvider.GetRequiredService<SigningKeyRingService>();
                await RotateIfDueAsync(keyRing, options, stoppingToken);
                await keyRing.CleanupAsync(stoppingToken);
            }

            var delay = TimeSpan.FromMinutes(Math.Max(1, options.CheckPeriodMinutes));
            _state.NextCheckUtc = DateTimeOffset.UtcNow.Add(delay);
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task RotateIfDueAsync(
        SigningKeyRingService keyRing,
        AuthServerSigningKeyOptions options,
        CancellationToken cancellationToken)
    {
        var active = await keyRing.GetActiveAsync(cancellationToken);
        if (active is null)
        {
            await keyRing.EnsureInitializedAsync(cancellationToken);
            return;
        }

        var activated = active.ActivatedUtc ?? active.CreatedUtc;
        var dueAt = activated.AddDays(Math.Max(1, options.RotationIntervalDays));
        if (DateTime.UtcNow < dueAt)
        {
            return;
        }

        var result = await keyRing.RotateNowAsync(cancellationToken);
        _optionsCache.TryRemove(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        _state.LastRotationUtc = DateTimeOffset.UtcNow;
        _logger.LogInformation(
            "Rotated signing keys. New active {NewKid}, previous {PreviousKid}.",
            result.NewActive.Kid,
            result.PreviousActive?.Kid);
    }
}
