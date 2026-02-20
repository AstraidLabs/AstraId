using Microsoft.IdentityModel.Tokens;

namespace AppServer.Security;

/// <summary>
/// Thread-safe in-memory lookup for internal token signing keys addressed by JWT <c>kid</c>.
/// </summary>
public sealed class InternalSigningKeyResolver
{
    // Keep signing keys in a kid-indexed dictionary for O(1) resolver lookups.
    private volatile IReadOnlyDictionary<string, SecurityKey> _keys = new Dictionary<string, SecurityKey>(StringComparer.Ordinal);

    // Whole-dictionary swap avoids partially updated key sets during concurrent token validation.
    public void UpdateKeys(IReadOnlyDictionary<string, SecurityKey> keys) => _keys = keys;

    public SecurityKey? Resolve(string? kid)
    {
        // Return null when kid is missing because no dictionary lookup can be performed.
        if (string.IsNullOrWhiteSpace(kid))
        {
            return null;
        }

        // Use TryGetValue to safely resolve a key without throwing for unknown kids.
        return _keys.TryGetValue(kid, out var key) ? key : null;
    }

    public bool HasKeys => _keys.Count > 0;
}
