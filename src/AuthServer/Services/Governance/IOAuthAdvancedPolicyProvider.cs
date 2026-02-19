namespace AuthServer.Services.Governance;

public interface IOAuthAdvancedPolicyProvider
{
    Task<OAuthAdvancedPolicySnapshot> GetCurrentAsync(CancellationToken cancellationToken);
    Task<OAuthAdvancedPolicySnapshot> UpdateAsync(OAuthAdvancedPolicySnapshot snapshot, string rowVersion, Guid? actorUserId, string? actorIp, CancellationToken cancellationToken);
    void InvalidateCache();
}
