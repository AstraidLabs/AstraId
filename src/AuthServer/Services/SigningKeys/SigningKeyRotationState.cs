namespace AuthServer.Services.SigningKeys;

/// <summary>
/// Provides signing key rotation state functionality.
/// </summary>
public sealed class SigningKeyRotationState : ISigningKeyRotationState
{
    public DateTimeOffset? NextCheckUtc { get; set; }
    public DateTimeOffset? LastRotationUtc { get; set; }
}
