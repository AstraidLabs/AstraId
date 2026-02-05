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

        options.AuthorizationCodeLifetime = TimeSpan.FromMinutes(policy.AuthorizationCodeMinutes);
        options.AccessTokenLifetime = TimeSpan.FromMinutes(policy.AccessTokenMinutes);
        options.IdentityTokenLifetime = TimeSpan.FromMinutes(policy.IdentityTokenMinutes);
        options.RefreshTokenLifetime = TimeSpan.FromDays(policy.RefreshTokenDays);
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(policy.ClockSkewSeconds);
    }
}
