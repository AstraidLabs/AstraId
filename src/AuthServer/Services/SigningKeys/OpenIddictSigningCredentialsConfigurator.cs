using System.Security.Cryptography;
using AuthServer.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace AuthServer.Services.SigningKeys;

public sealed class OpenIddictSigningCredentialsConfigurator : IConfigureOptions<OpenIddictServerOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public OpenIddictSigningCredentialsConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Configure(OpenIddictServerOptions options)
    {
        using var scope = _serviceProvider.CreateScope();
        var keyRing = scope.ServiceProvider.GetRequiredService<SigningKeyRingService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OpenIddictSigningCredentialsConfigurator>>();
        var keys = keyRing.GetCurrentAsync(CancellationToken.None).GetAwaiter().GetResult();
        if (keys.Count == 0)
        {
            var active = keyRing.EnsureInitializedAsync(CancellationToken.None).GetAwaiter().GetResult();
            keys = new List<SigningKeyRingEntry> { active };
        }

        options.SigningCredentials.Clear();
        var activeEntry = keys.FirstOrDefault(entry => entry.Status == SigningKeyStatus.Active);
        var activeAdded = false;
        var activeFailed = false;
        foreach (var entry in keys.OrderBy(entry => entry.Status))
        {
            try
            {
                options.SigningCredentials.Add(keyRing.CreateSigningCredentials(entry));
                if (entry.Status == SigningKeyStatus.Active)
                {
                    activeAdded = true;
                }
            }
            catch (CryptographicException ex)
            {
                logger.LogError(ex, "Failed to unprotect signing key material for {Kid}.", entry.Kid);
                if (entry.Status == SigningKeyStatus.Active)
                {
                    activeFailed = true;
                }
            }
        }

        if (!activeAdded)
        {
            if (activeEntry is not null && activeFailed)
            {
                keyRing.RevokeAsync(activeEntry.Kid, CancellationToken.None).GetAwaiter().GetResult();
                var refreshedActive = keyRing.GetActiveAsync(CancellationToken.None).GetAwaiter().GetResult()
                    ?? keyRing.EnsureInitializedAsync(CancellationToken.None).GetAwaiter().GetResult();
                options.SigningCredentials.Add(keyRing.CreateSigningCredentials(refreshedActive));
                logger.LogWarning("Rotated signing key after failing to unprotect the active key material.");
            }
            else if (options.SigningCredentials.Count == 0)
            {
                var refreshedActive = keyRing.EnsureInitializedAsync(CancellationToken.None).GetAwaiter().GetResult();
                options.SigningCredentials.Add(keyRing.CreateSigningCredentials(refreshedActive));
                logger.LogWarning("Initialized a new signing key because none could be loaded.");
            }
        }
    }
}
