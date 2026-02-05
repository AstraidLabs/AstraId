namespace AuthServer.Services.SigningKeys;

public interface ISigningKeyRotationState
{
    DateTimeOffset? NextCheckUtc { get; set; }
    DateTimeOffset? LastRotationUtc { get; set; }
}
