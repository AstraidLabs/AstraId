namespace AuthServer.Services.Governance;

/// <summary>
/// Defines the contract for o auth advanced policy provider.
/// </summary>
public interface IOAuthAdvancedPolicyProvider
{
    Task<OAuthAdvancedPolicySnapshot> GetCurrentAsync(CancellationToken cancellationToken);
    Task<OAuthAdvancedPolicySnapshot> UpdateAsync(OAuthAdvancedPolicySnapshot snapshot, string rowVersion, Guid? actorUserId, string? actorIp, CancellationToken cancellationToken);
    void InvalidateCache();
}
