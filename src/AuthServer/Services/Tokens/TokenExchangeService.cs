using System.Security.Claims;
using AuthServer.Options;
using Microsoft.Extensions.Options;
using OpenIddict.Validation;

namespace AuthServer.Services.Tokens;

public sealed class TokenExchangeService
{
    public const string GrantType = "urn:ietf:params:oauth:grant-type:token-exchange";

    private readonly TokenExchangeOptions _options;
    private readonly IOpenIddictValidationService _validationService;

    public TokenExchangeService(
        IOptions<TokenExchangeOptions> options,
        IOpenIddictValidationService validationService)
    {
        _options = options.Value;
        _validationService = validationService;
    }

    public bool Enabled => _options.Enabled;

    public bool IsClientAllowed(string? clientId)
        => !string.IsNullOrWhiteSpace(clientId)
           && _options.AllowedClients.Contains(clientId, StringComparer.OrdinalIgnoreCase);

    public bool IsAudienceAllowed(string? audience)
        => !string.IsNullOrWhiteSpace(audience)
           && _options.AllowedAudiences.Contains(audience, StringComparer.OrdinalIgnoreCase);

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
