using System.Security.Cryptography;
using Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Security;

/// <summary>
/// Provides internal token key ring service functionality.
/// </summary>
public sealed class InternalTokenKeyRingService
{
    private readonly IOptionsMonitor<InternalTokenOptions> _options;
    private readonly ILogger<InternalTokenKeyRingService> _logger;
    private readonly object _gate = new();
    private InternalTokenKeyRing _ring;

    public InternalTokenKeyRingService(IOptionsMonitor<InternalTokenOptions> options, ILogger<InternalTokenKeyRingService> logger)
    {
        _options = options;
        _logger = logger;
        _ring = CreateInitialRing(options.CurrentValue);
    }

    public InternalSigningKey GetCurrentKey()
    {
        lock (_gate)
        {
            return _ring.Current;
        }
    }

    public IReadOnlyCollection<InternalSigningKey> GetPublicKeys()
    {
        lock (_gate)
        {
            return [.. _ring.Keys.Values];
        }
    }

    public void RotateIfDue()
    {
        lock (_gate)
        {
            var options = _options.CurrentValue;
            // Skip rotation entirely when key rotation is disabled by configuration.
            if (!options.Signing.RotationEnabled)
            {
                return;
            }

            var nextRotationDue = _ring.Current.CreatedUtc.AddDays(options.Signing.RotationIntervalDays);
            // Keep the current key until the configured rotation interval elapses.
            if (DateTimeOffset.UtcNow < nextRotationDue)
            {
                return;
            }

            var newKey = GenerateKey(options);
            // Store the new key by kid so validators can resolve it from JWT headers.
            _ring.Keys[newKey.Kid] = newKey;
            _ring.CurrentKid = newKey.Kid;
            CleanupOldKeys(options);

            _logger.LogInformation("Internal token key rotated. New kid: {Kid}, rotated at: {RotatedAtUtc}", newKey.Kid, newKey.CreatedUtc);
        }
    }

    private static InternalTokenKeyRing CreateInitialRing(InternalTokenOptions options)
    {
        var key = GenerateKey(options);
        return new InternalTokenKeyRing
        {
            CurrentKid = key.Kid,
            // Map key id to signing key for fast lookup by JWT kid.
            Keys = new Dictionary<string, InternalSigningKey>(StringComparer.Ordinal)
            {
                // Seed the ring with the initial signing key.
                [key.Kid] = key
            }
        };
    }

    private static InternalSigningKey GenerateKey(InternalTokenOptions options)
    {
        // Select key creation strategy based on configured signing algorithm.
        return ResolveAlgorithm(options.Signing.Algorithm) switch
        {
            // Use RSA keys when RS256 is configured.
            SecurityAlgorithms.RsaSha256 => CreateRsaKey(options.Signing.KeySize),
            // Use ECDSA keys when ES256 is configured.
            SecurityAlgorithms.EcdsaSha256 => CreateEcdsaKey(),
            _ => throw new InvalidOperationException("Unsupported internal token signing algorithm.")
        };
    }

    private static InternalSigningKey CreateRsaKey(int keySize)
    {
        var rsa = RSA.Create(Math.Max(2048, keySize));
        var securityKey = new RsaSecurityKey(rsa)
        {
            KeyId = $"rsa-{Guid.NewGuid():N}"
        };

        return new InternalSigningKey(securityKey.KeyId!, SecurityAlgorithms.RsaSha256, securityKey, DateTimeOffset.UtcNow);
    }

    private static InternalSigningKey CreateEcdsaKey()
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var securityKey = new ECDsaSecurityKey(ecdsa)
        {
            KeyId = $"ec-{Guid.NewGuid():N}"
        };

        return new InternalSigningKey(securityKey.KeyId!, SecurityAlgorithms.EcdsaSha256, securityKey, DateTimeOffset.UtcNow);
    }

    // Map configured algorithm aliases to IdentityModel algorithm constants.
    private static string ResolveAlgorithm(string algorithm) => algorithm.ToUpperInvariant() switch
    {
        // Resolve RS256 alias to RSA-SHA256.
        "RS256" => SecurityAlgorithms.RsaSha256,
        // Resolve ES256 alias to ECDSA-SHA256.
        "ES256" => SecurityAlgorithms.EcdsaSha256,
        _ => throw new InvalidOperationException("Unsupported internal token algorithm.")
    };

    private void CleanupOldKeys(InternalTokenOptions options)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-options.Signing.PreviousKeyRetentionDays);
        // Collect non-current keys older than retention window for cleanup.
        var removableKids = _ring.Keys.Values
            .Where(key => !string.Equals(key.Kid, _ring.CurrentKid, StringComparison.Ordinal) && key.CreatedUtc < cutoff)
            .Select(key => key.Kid)
            .ToArray();

        // Remove expired key entries from the key dictionary by kid.
        foreach (var kid in removableKids)
        {
            _ring.Keys.Remove(kid);
        }
    }

    /// <summary>
    /// Provides internal token key ring functionality.
    /// </summary>
    private sealed class InternalTokenKeyRing
    {
        public required string CurrentKid { get; set; }
        public required Dictionary<string, InternalSigningKey> Keys { get; set; }

        // Use current kid as dictionary index to return the active signing key.
        public InternalSigningKey Current => Keys[CurrentKid];
    }
}

/// <summary>
/// Provides internal signing key functionality.
/// </summary>
public sealed record InternalSigningKey(string Kid, string Algorithm, SecurityKey PrivateKey, DateTimeOffset CreatedUtc);
