namespace AuthServer.Options;

public sealed class AuthServerTokenOptions
{
    public const string SectionName = "AuthServer:Tokens";

    public TokenPresetOptions Public { get; set; } = new();
    public TokenPresetOptions Confidential { get; set; } = new();
    public RefreshTokenPolicyOptions RefreshPolicy { get; set; } = new();

    public sealed class TokenPresetOptions
    {
        public int AccessTokenMinutes { get; set; } = 30;
        public int IdentityTokenMinutes { get; set; } = 30;
        public int RefreshTokenAbsoluteDays { get; set; } = 30;
        public int RefreshTokenSlidingDays { get; set; } = 7;
    }

    public sealed class RefreshTokenPolicyOptions
    {
        public bool RotationEnabled { get; set; } = true;
        public bool ReuseDetectionEnabled { get; set; } = true;
        public int ReuseLeewaySeconds { get; set; } = 30;
    }
}
