using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Services.SigningKeys;

public static class SigningKeyJwkBuilder
{
    public static JsonWebKey FromCertificate(X509Certificate2 certificate, string algorithm)
    {
        using var rsa = certificate.GetRSAPublicKey()
            ?? throw new InvalidOperationException("Signing certificate does not include an RSA public key.");
        var parameters = rsa.ExportParameters(false);
        var kid = Base64UrlEncoder.Encode(certificate.GetCertHash());
        return FromRsaParameters(parameters, kid, algorithm);
    }

    public static JsonWebKey FromRsaParameters(RSAParameters parameters, string kid, string algorithm)
    {
        return new JsonWebKey
        {
            Kty = "RSA",
            Use = "sig",
            Alg = algorithm,
            Kid = kid,
            N = Base64UrlEncoder.Encode(parameters.Modulus),
            E = Base64UrlEncoder.Encode(parameters.Exponent)
        };
    }
}
