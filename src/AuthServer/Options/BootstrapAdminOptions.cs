namespace AuthServer.Options;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public bool Enabled { get; set; }
    public bool OnlyInDevelopment { get; set; }
    public string RoleName { get; set; } = "Admin";
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool RequireConfirmedEmail { get; set; } = true;
    public bool GeneratePasswordWhenMissing { get; set; }
}
