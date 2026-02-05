namespace AuthServer.Services.SigningKeys;

public sealed class SigningKeyRotationState : ISigningKeyRotationState
{
    public DateTimeOffset? NextCheckUtc { get; set; }
    public DateTimeOffset? LastRotationUtc { get; set; }
}
