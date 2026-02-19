using Microsoft.IdentityModel.Tokens;

namespace AppServer.Security;

public sealed class InternalSigningKeyResolver
{
    private volatile IReadOnlyDictionary<string, SecurityKey> _keys = new Dictionary<string, SecurityKey>(StringComparer.Ordinal);

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
