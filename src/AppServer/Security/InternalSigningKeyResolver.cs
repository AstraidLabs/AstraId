using Microsoft.IdentityModel.Tokens;

namespace AppServer.Security;

/// <summary>
/// Thread-safe in-memory lookup for internal token signing keys addressed by JWT <c>kid</c>.
/// </summary>
public sealed class InternalSigningKeyResolver
{
    private volatile IReadOnlyDictionary<string, SecurityKey> _keys = new Dictionary<string, SecurityKey>(StringComparer.Ordinal);

    // Whole-dictionary swap avoids partially updated key sets during concurrent token validation.
    public void UpdateKeys(IReadOnlyDictionary<string, SecurityKey> keys) => _keys = keys;

    public SecurityKey? Resolve(string? kid)
    {
        if (string.IsNullOrWhiteSpace(kid))
        {
            return null;
        }

        return _keys.TryGetValue(kid, out var key) ? key : null;
    }

    public bool HasKeys => _keys.Count > 0;
}
