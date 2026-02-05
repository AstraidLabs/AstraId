namespace AuthServer.Services.Admin.Models;

public sealed record AdminRevocationResult(
    int TokensRevoked,
    int AuthorizationsRevoked);
