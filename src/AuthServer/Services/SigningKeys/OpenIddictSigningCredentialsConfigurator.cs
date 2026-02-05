using AuthServer.Data;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace AuthServer.Services.SigningKeys;

public sealed class OpenIddictSigningCredentialsConfigurator : IConfigureOptions<OpenIddictServerOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public OpenIddictSigningCredentialsConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Configure(OpenIddictServerOptions options)
    {
        using var scope = _serviceProvider.CreateScope();
        var keyRing = scope.ServiceProvider.GetRequiredService<SigningKeyRingService>();
        var keys = keyRing.GetCurrentAsync(CancellationToken.None).GetAwaiter().GetResult();
        if (keys.Count == 0)
        {
            var active = keyRing.EnsureInitializedAsync(CancellationToken.None).GetAwaiter().GetResult();
            keys = new List<SigningKeyRingEntry> { active };
        }

        options.SigningCredentials.Clear();
        foreach (var entry in keys.OrderBy(entry => entry.Status))
        {
            options.SigningCredentials.Add(keyRing.CreateSigningCredentials(entry));
        }
    }
}
