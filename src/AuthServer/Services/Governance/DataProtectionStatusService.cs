using AuthServer.Options;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Governance;

/// <summary>
/// Provides data protection status functionality.
/// </summary>
public sealed record DataProtectionStatus(
    bool KeysPersisted,
    string? KeyPath,
    bool ReadOnly,
    int KeyCount,
    DateTime? LatestKeyActivationUtc,
    DateTime? LatestKeyExpirationUtc);

/// <summary>
/// Provides data protection status service functionality.
/// </summary>
public sealed class DataProtectionStatusService
{
    private readonly IKeyManager _keyManager;
    private readonly IOptions<AuthServerDataProtectionOptions> _options;

    public DataProtectionStatusService(IKeyManager keyManager, IOptions<AuthServerDataProtectionOptions> options)
    {
        _keyManager = keyManager;
        _options = options;
    }

    public DataProtectionStatus GetStatus()
    {
        var keys = _keyManager.GetAllKeys();
        var latestActivation = keys.OrderByDescending(key => key.ActivationDate).FirstOrDefault()?.ActivationDate.UtcDateTime;
        var latestExpiration = keys.OrderByDescending(key => key.ExpirationDate).FirstOrDefault()?.ExpirationDate.UtcDateTime;

        var path = _options.Value.KeyPath;
        return new DataProtectionStatus(
            KeysPersisted: !string.IsNullOrWhiteSpace(path),
            KeyPath: path,
            ReadOnly: _options.Value.ReadOnly,
            KeyCount: keys.Count,
            LatestKeyActivationUtc: latestActivation,
            LatestKeyExpirationUtc: latestExpiration);
    }
}
