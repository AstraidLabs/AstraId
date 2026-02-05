using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace AuthServer.Services.Tokens;

public sealed class OpenIddictTokenPolicyConfigurator : IConfigureOptions<OpenIddictServerOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public OpenIddictTokenPolicyConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Configure(OpenIddictServerOptions options)
    {
        using var scope = _serviceProvider.CreateScope();
        var policyService = scope.ServiceProvider.GetRequiredService<TokenPolicyService>();
        var policy = policyService.GetEffectivePolicyAsync(CancellationToken.None).GetAwaiter().GetResult();

        options.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(policy.AuthorizationCodeMinutes));
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(policy.AccessTokenMinutes));
        options.SetIdentityTokenLifetime(TimeSpan.FromMinutes(policy.IdentityTokenMinutes));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(policy.RefreshTokenDays));
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(policy.ClockSkewSeconds);
    }
}
