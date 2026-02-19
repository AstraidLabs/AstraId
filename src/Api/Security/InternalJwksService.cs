using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Api.Security;

public sealed class InternalJwksService
{
    private readonly InternalTokenKeyRingService _keyRingService;

    public InternalJwksService(InternalTokenKeyRingService keyRingService)
    {
        _keyRingService = keyRingService;
    }

    public object GetJwksDocument()
    {
        var keys = _keyRingService.GetPublicKeys()
            .Select(ToPublicJwk)
            .ToArray();

        return new { keys };
    }

    private static JsonWebKey ToPublicJwk(InternalSigningKey key)
    {
        var jwk = key.PrivateKey switch
        {
            RsaSecurityKey rsaKey => JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(rsaKey.Rsa?.ExportParameters(false) ?? rsaKey.Parameters)),
            ECDsaSecurityKey ecKey => JsonWebKeyConverter.ConvertFromECDsaSecurityKey(CreatePublicEcKey(ecKey)),
            _ => throw new InvalidOperationException("Unsupported key type for JWKS export.")
        };

        jwk.Kid = key.Kid;
        jwk.Use = "sig";
        jwk.KeyOps.Add("verify");
        jwk.Alg = key.Algorithm == SecurityAlgorithms.EcdsaSha256 ? "ES256" : "RS256";
        return jwk;
    }

    private static ECDsaSecurityKey CreatePublicEcKey(ECDsaSecurityKey key)
    {
        var publicParameters = key.ECDsa!.ExportParameters(false);
        var publicEcdsa = ECDsa.Create(publicParameters);
        return new ECDsaSecurityKey(publicEcdsa);
    }
}
