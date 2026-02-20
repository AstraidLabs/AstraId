using AuthServer.Options;

namespace AuthServer.Services.SigningKeys;

/// <summary>
/// Provides signing key mode resolver functionality.
/// </summary>
public static class SigningKeyModeResolver
{
    public static SigningKeyMode Resolve(SigningKeyMode mode, IHostEnvironment environment)
    {
        if (mode != SigningKeyMode.Auto)
        {
            return mode;
        }

        return environment.IsDevelopment() ? SigningKeyMode.DbKeyRing : SigningKeyMode.Certificates;
    }
}
