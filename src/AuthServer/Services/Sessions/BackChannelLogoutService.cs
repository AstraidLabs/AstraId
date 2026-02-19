using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using AuthServer.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Services.Sessions;

public sealed class BackChannelLogoutService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SessionManagementOptions _options;
    private readonly string? _issuer;
    private readonly SigningCredentials? _signingCredentials;
    private readonly ILogger<BackChannelLogoutService> _logger;

    public BackChannelLogoutService(
        IHttpClientFactory httpClientFactory,
        IOptions<SessionManagementOptions> options,
        IOptions<OpenIddict.Server.OpenIddictServerOptions> serverOptions,
        ILogger<BackChannelLogoutService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;

        var tokenValidation = serverOptions.Value.TokenValidationParameters;
        _issuer = tokenValidation.ValidIssuer;
        var signingKey = tokenValidation.IssuerSigningKey ?? tokenValidation.IssuerSigningKeys?.FirstOrDefault();
        if (signingKey is not null)
        {
            _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
        }
    }

    public bool Enabled => _options.BackChannelEnabled;

    public async Task NotifyAsync(string subject, IEnumerable<string> clientIds, CancellationToken cancellationToken)
    {
        if (!_options.BackChannelEnabled || _signingCredentials is null)
        {
            return;
        }

        foreach (var clientId in clientIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!_options.BackChannelLogoutUrls.TryGetValue(clientId, out var url) || string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var logoutToken = BuildLogoutToken(subject, clientId);
            var body = new Dictionary<string, string> { ["logout_token"] = logoutToken };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new FormUrlEncodedContent(body)
                };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var client = _httpClientFactory.CreateClient(nameof(BackChannelLogoutService));
                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Back-channel logout call failed for client {ClientId} with status {StatusCode}.", clientId, (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Back-channel logout call failed for client {ClientId}.", clientId);
            }
        }
    }

    private string BuildLogoutToken(string subject, string audience)
    {
        var now = DateTimeOffset.UtcNow;
        var tokenHandler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", subject),
                new Claim("events", "{\"http://schemas.openid.net/event/backchannel-logout\":{}}", "JSON"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            }),
            Audience = audience,
            Issuer = _issuer,
            Expires = now.AddMinutes(5).UtcDateTime,
            NotBefore = now.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            SigningCredentials = _signingCredentials
        };

        var token = tokenHandler.CreateJwtSecurityToken(descriptor);
        return tokenHandler.WriteToken(token);
    }
}
