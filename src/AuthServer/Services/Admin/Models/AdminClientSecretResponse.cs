namespace AuthServer.Services.Admin.Models;

public sealed record AdminClientSecretResponse(AdminClientDetail Client, string? ClientSecret);
