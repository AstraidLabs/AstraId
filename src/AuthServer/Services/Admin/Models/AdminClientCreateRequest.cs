namespace AuthServer.Services.Admin.Models;

public sealed class AdminClientCreateRequest
{
    public string? ClientId { get; set; }
    public string? DisplayName { get; set; }
    public string? ClientType { get; set; }
    public bool Enabled { get; set; } = true;
    public IReadOnlyList<string> GrantTypes { get; set; } = Array.Empty<string>();
    public bool PkceRequired { get; set; }
    public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> RedirectUris { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> PostLogoutRedirectUris { get; set; } = Array.Empty<string>();
}
