namespace AuthServer.Services.SigningKeys;

public interface ISigningKeyProtector
{
    string Protect(string value);
    string Unprotect(string value);
}
