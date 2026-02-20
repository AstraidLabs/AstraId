namespace AuthServer.Services.SigningKeys;

/// <summary>
/// Defines the contract for signing key protector.
/// </summary>
public interface ISigningKeyProtector
{
    string Protect(string value);
    string Unprotect(string value);
}
