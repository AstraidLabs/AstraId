using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Options;
using Company.Auth.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Security;

/// <summary>
/// Issues short-lived internal service tokens for AppServer calls on behalf of an authenticated API user context.
/// </summary>
public interface IInternalTokenService
{
    string CreateToken(ClaimsPrincipal principal, IEnumerable<string> grantedScopes);
}

/// <summary>
/// Builds signed JWTs that carry only claims required for internal authorization and audit correlation.
/// </summary>
public sealed class InternalTokenService : IInternalTokenService
{
    private readonly IOptions<InternalTokenOptions> _options;
    private readonly InternalTokenKeyRingService _keyRingService;

    public InternalTokenService(IOptions<InternalTokenOptions> options, InternalTokenKeyRingService keyRingService)
    {
        _options = options;
        _keyRingService = keyRingService;
    }

    /// <summary>
    /// Creates an internal token whose lifetime is configured in seconds via <c>InternalTokens:TokenTtlSeconds</c>.
    /// </summary>
    public string CreateToken(ClaimsPrincipal principal, IEnumerable<string> grantedScopes)
    {
        var settings = _options.Value;
        var subject = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("Cannot issue internal token without subject.");
        }

        var now = DateTime.UtcNow;
        var signingKey = _keyRingService.GetCurrentKey();
        var credentials = new SigningCredentials(
            signingKey.PrivateKey,
            signingKey.Algorithm);

        var scopes = grantedScopes
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Service identity claims let AppServer distinguish trusted Api calls from end-user access tokens.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
            new("svc", "api"),
            new(JwtRegisteredClaimNames.Azp, "api")
        };

        var tenant = principal.FindFirst("tenant")?.Value;
        if (!string.IsNullOrWhiteSpace(tenant))
        {
            claims.Add(new Claim("tenant", tenant));
        }

        foreach (var permission in principal.FindAll(AuthConstants.ClaimTypes.Permission).Select(claim => claim.Value).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(AuthConstants.ClaimTypes.Permission, permission));
        }

        if (scopes.Length > 0)
        {
            claims.Add(new Claim("scope", string.Join(' ', scopes)));
        }

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddSeconds(settings.TokenTtlSeconds),
            signingCredentials: credentials);

        token.Header[JwtHeaderParameterNames.Kid] = signingKey.Kid;

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
