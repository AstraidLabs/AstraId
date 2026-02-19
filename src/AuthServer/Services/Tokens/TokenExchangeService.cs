using System.Security.Claims;
using AuthServer.Options;
using AuthServer.Services.Governance;
using OpenIddict.Validation;

namespace AuthServer.Services.Tokens;

public sealed class TokenExchangeService
{
    public const string GrantType = "urn:ietf:params:oauth:grant-type:token-exchange";

    private readonly TokenExchangeOptions _options;
    private readonly IOpenIddictValidationService _validationService;
    private readonly IOAuthAdvancedPolicyProvider _policyProvider;

    public TokenExchangeService(
        Microsoft.Extensions.Options.IOptions<TokenExchangeOptions> options,
        IOpenIddictValidationService validationService,
        IOAuthAdvancedPolicyProvider policyProvider)
    {
        _options = options.Value;
        _validationService = validationService;
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

        return await _validationService.ValidateAccessTokenAsync(subjectToken, cancellationToken);
    }

    public string ActorClaimType => _options.ActorClaimType;
}
