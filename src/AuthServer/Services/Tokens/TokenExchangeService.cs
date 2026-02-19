using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using AuthServer.Options;
using AuthServer.Services.Governance;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace AuthServer.Services.Tokens;

public sealed class TokenExchangeService
{
    public const string GrantType = "urn:ietf:params:oauth:grant-type:token-exchange";

    private readonly TokenExchangeOptions _options;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IOAuthAdvancedPolicyProvider _policyProvider;

    public TokenExchangeService(
        Microsoft.Extensions.Options.IOptions<TokenExchangeOptions> options,
        Microsoft.Extensions.Options.IOptions<OpenIddictServerOptions> serverOptions,
        IOAuthAdvancedPolicyProvider policyProvider)
    {
        _options = options.Value;
        _tokenValidationParameters = serverOptions.Value.TokenValidationParameters.Clone();
        _tokenValidationParameters.ValidateAudience = false;
        _policyProvider = policyProvider;
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken)
        => (await _policyProvider.GetCurrentAsync(cancellationToken)).TokenExchangeEnabled;

    public async Task<bool> IsClientAllowedAsync(string? clientId, CancellationToken cancellationToken)
    {
        var policy = await _policyProvider.GetCurrentAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(clientId)
           && policy.TokenExchangeAllowedClientIds.Contains(clientId, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> IsAudienceAllowedAsync(string? audience, CancellationToken cancellationToken)
    {
        var policy = await _policyProvider.GetCurrentAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(audience)
           && policy.TokenExchangeAllowedAudiences.Contains(audience, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ClaimsPrincipal?> ValidateSubjectTokenAsync(string? subjectToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subjectToken))
        {
            return null;
        }

        try
        {
            var principal = _tokenHandler.ValidateToken(subjectToken, _tokenValidationParameters, out _);
            return await Task.FromResult(principal);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    public string ActorClaimType => _options.ActorClaimType;
}
