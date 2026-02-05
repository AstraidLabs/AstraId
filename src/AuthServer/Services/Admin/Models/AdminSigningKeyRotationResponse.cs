namespace AuthServer.Services.Admin.Models;

public sealed record AdminSigningKeyRotationResponse(
    string NewActiveKid,
    string? PreviousKid,
    DateTime ActivatedUtc);
