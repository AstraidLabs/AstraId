namespace Api.Models;

public sealed class UserProfileModel
{
    public string Sub { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
}
