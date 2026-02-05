namespace AuthServer.Data;

public sealed class TokenPolicyOverride
{
    public Guid Id { get; set; }
    public int? PublicAccessTokenMinutes { get; set; }
    public int? PublicIdentityTokenMinutes { get; set; }
    public int? PublicRefreshTokenAbsoluteDays { get; set; }
    public int? PublicRefreshTokenSlidingDays { get; set; }
    public int? ConfidentialAccessTokenMinutes { get; set; }
    public int? ConfidentialIdentityTokenMinutes { get; set; }
    public int? ConfidentialRefreshTokenAbsoluteDays { get; set; }
    public int? ConfidentialRefreshTokenSlidingDays { get; set; }
    public bool? RefreshRotationEnabled { get; set; }
    public bool? RefreshReuseDetectionEnabled { get; set; }
    public int? RefreshReuseLeewaySeconds { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
