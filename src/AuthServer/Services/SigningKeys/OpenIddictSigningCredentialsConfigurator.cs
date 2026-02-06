using System.Security.Cryptography;
using AuthServer.Data;
using AuthServer.Options;
using AuthServer.Services.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
        var signingOptions = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<AuthServerSigningKeyOptions>>();
        var certificateOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthServerCertificateOptions>>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OpenIddictSigningCredentialsConfigurator>>();
        var mode = SigningKeyModeResolver.Resolve(signingOptions.CurrentValue.Mode, environment);

        options.SigningCredentials.Clear();
        options.JsonWebKeySet ??= new JsonWebKeySet();
        options.JsonWebKeySet.Keys.Clear();

        if (mode == SigningKeyMode.Certificates)
        {
            var configured = ConfigureCertificateSigning(options, signingOptions.CurrentValue, certificateOptions.Value);
            if (configured)
            {
                return;
            }
        }

        ConfigureDbKeyRingSigning(options, keyRing, logger);
    }

    private static bool ConfigureCertificateSigning(
        OpenIddictServerOptions options,
        AuthServerSigningKeyOptions signingOptions,
        AuthServerCertificateOptions certificateOptions)
    {
        var certificate = CertificateLoader.TryLoadCertificate(certificateOptions.Signing);
        if (certificate is null)
        {
            throw new InvalidOperationException("Signing certificate is required when SigningKeys:Mode is Certificates.");
        }

        var key = new X509SecurityKey(certificate)
        {
            KeyId = Base64UrlEncoder.Encode(certificate.GetCertHash())
        };
        options.SigningCredentials.Add(new SigningCredentials(key, signingOptions.Algorithm));
        options.JsonWebKeySet.Keys.Add(SigningKeyJwkBuilder.FromCertificate(certificate, signingOptions.Algorithm));
        return true;
    }

    private static void ConfigureDbKeyRingSigning(
        OpenIddictServerOptions options,
        SigningKeyRingService keyRing,
        ILogger logger)
    {
        var keys = keyRing.GetCurrentAsync(CancellationToken.None).GetAwaiter().GetResult();
        if (keys.Count == 0)
        {
            var active = keyRing.EnsureInitializedAsync(CancellationToken.None).GetAwaiter().GetResult();
            keys = new List<SigningKeyRingEntry> { active };
        }

        var activeEntry = keys.FirstOrDefault(entry => entry.Status == SigningKeyStatus.Active);
        var activeAdded = false;
        var activeFailed = false;

        if (activeEntry is not null)
        {
            try
            {
                options.SigningCredentials.Add(keyRing.CreateSigningCredentials(activeEntry));
                activeAdded = true;
            }
            catch (CryptographicException ex)
            {
                logger.LogError(ex, "Failed to unprotect signing key material for {Kid}.", activeEntry.Kid);
                activeFailed = true;
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

        foreach (var entry in keys)
        {
            options.JsonWebKeySet.Keys.Add(new JsonWebKey(entry.PublicJwkJson));
        }
    }
}
