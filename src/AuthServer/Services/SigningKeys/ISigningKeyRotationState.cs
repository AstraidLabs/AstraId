namespace AuthServer.Services.SigningKeys;

/// <summary>
/// Defines the contract for signing key rotation state.
/// </summary>
public interface ISigningKeyRotationState
{
    DateTimeOffset? NextCheckUtc { get; set; }
    DateTimeOffset? LastRotationUtc { get; set; }
}
