using AuthServer.Options;

namespace AuthServer.Services.SigningKeys;

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
