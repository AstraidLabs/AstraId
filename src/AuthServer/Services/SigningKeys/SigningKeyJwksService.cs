using System.Text.Json;
using AuthServer.Data;
using AuthServer.Options;
using AuthServer.Services.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Services.SigningKeys;

/// <summary>
/// Provides signing key jwks service functionality.
/// </summary>
public sealed class SigningKeyJwksService
{
    private readonly SigningKeyRingService _keyRingService;
    private readonly IOptionsMonitor<AuthServerSigningKeyOptions> _signingOptions;
    private readonly IOptions<AuthServerCertificateOptions> _certificateOptions;
    private readonly IHostEnvironment _environment;

    public SigningKeyJwksService(
        SigningKeyRingService keyRingService,
        IOptionsMonitor<AuthServerSigningKeyOptions> signingOptions,
        IOptions<AuthServerCertificateOptions> certificateOptions,
        IHostEnvironment environment)
    {
        _keyRingService = keyRingService;
        _signingOptions = signingOptions;
        _certificateOptions = certificateOptions;
        _environment = environment;
    }

    public async Task<string> BuildPublicJwksJsonAsync(CancellationToken cancellationToken)
    {
        var jwks = await BuildPublicJwksAsync(cancellationToken);
        return JsonSerializer.Serialize(jwks, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<JsonWebKeySet> BuildPublicJwksAsync(CancellationToken cancellationToken)
    {
        var jwks = new JsonWebKeySet();
        var mode = SigningKeyModeResolver.Resolve(_signingOptions.CurrentValue.Mode, _environment);

        if (mode == SigningKeyMode.DbKeyRing)
        {
            var keys = await _keyRingService.GetCurrentAsync(cancellationToken);
            if (keys.Count == 0)
            {
                var active = await _keyRingService.EnsureInitializedAsync(cancellationToken);
                keys = new List<SigningKeyRingEntry> { active };
            }

            foreach (var entry in keys)
            {
                jwks.Keys.Add(new JsonWebKey(entry.PublicJwkJson));
            }

            return jwks;
        }

        var certificate = CertificateLoader.TryLoadCertificate(_certificateOptions.Value.Signing);
        if (certificate is null)
        {
            return jwks;
        }

        jwks.Keys.Add(SigningKeyJwkBuilder.FromCertificate(certificate, _signingOptions.CurrentValue.Algorithm));
        return jwks;
    }
}
