using System.Security.Cryptography;
using AuthServer.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Security;

public sealed class ProtectedUserStore : UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>
{
    private const string InternalLoginProvider = "[AspNetUserStore]";
    private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
    private const string RecoveryCodeTokenName = "RecoveryCodes";
    private const string ProtectedPrefix = "dpv1:";

    private readonly IDataProtector _protector;

    public ProtectedUserStore(
        ApplicationDbContext context,
        IdentityErrorDescriber? describer,
        IDataProtectionProvider dataProtectionProvider)
        : base(context, describer)
    {
        _protector = dataProtectionProvider.CreateProtector("AuthServer.IdentityTokens.MfaSecrets");
    }

    public override async Task SetTokenAsync(ApplicationUser user, string loginProvider, string name, string? value, CancellationToken cancellationToken = default)
    {
        if (ShouldProtect(loginProvider, name) && !string.IsNullOrEmpty(value))
        {
            value = Protect(value);
        }

        await base.SetTokenAsync(user, loginProvider, name, value, cancellationToken);
    }

    public override async Task<string?> GetTokenAsync(ApplicationUser user, string loginProvider, string name, CancellationToken cancellationToken = default)
    {
        var value = await base.GetTokenAsync(user, loginProvider, name, cancellationToken);
        if (!ShouldProtect(loginProvider, name) || string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
        {
            try
            {
                return _protector.Unprotect(value[ProtectedPrefix.Length..]);
            }
            catch (CryptographicException)
            {
                return value;
            }
        }

        await MigrateLegacyValueAsync(user, loginProvider, name, value, cancellationToken);
        return value;
    }

    private async Task MigrateLegacyValueAsync(ApplicationUser user, string loginProvider, string name, string value, CancellationToken cancellationToken)
    {
        await base.SetTokenAsync(user, loginProvider, name, Protect(value), cancellationToken);
        try
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
        }
    }

    private static bool ShouldProtect(string loginProvider, string name)
    {
        return string.Equals(loginProvider, InternalLoginProvider, StringComparison.Ordinal)
            && (string.Equals(name, AuthenticatorKeyTokenName, StringComparison.Ordinal)
                || string.Equals(name, RecoveryCodeTokenName, StringComparison.Ordinal));
    }

    private string Protect(string value) => $"{ProtectedPrefix}{_protector.Protect(value)}";
}
