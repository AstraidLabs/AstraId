namespace AuthServer.Services.Admin.Models;

public sealed record AdminClientSecretResult(AdminClientDetail Client, string? ClientSecret);
