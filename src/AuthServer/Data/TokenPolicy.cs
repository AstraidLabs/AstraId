namespace AuthServer.Data;

public sealed class TokenPolicy
{
    public Guid Id { get; set; }
    public int AccessTokenMinutes { get; set; }
    public int IdentityTokenMinutes { get; set; }
    public int AuthorizationCodeMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
    public bool RefreshRotationEnabled { get; set; }
    public bool RefreshReuseDetectionEnabled { get; set; }
    public int RefreshReuseLeewaySeconds { get; set; }
    public int ClockSkewSeconds { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
