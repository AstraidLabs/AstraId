using Microsoft.AspNetCore.DataProtection;

namespace AuthServer.Services.SigningKeys;

public sealed class SigningKeyProtector : ISigningKeyProtector
{
    private readonly IDataProtector _protector;

    public SigningKeyProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("AuthServer.SigningKeys");
    }

    public string Protect(string value) => _protector.Protect(value);

    public string Unprotect(string value) => _protector.Unprotect(value);
}
