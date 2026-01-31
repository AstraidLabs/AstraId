namespace Api.Contracts;

public sealed class MeDto
{
    public string Sub { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string[] Permissions { get; set; } = [];
}
