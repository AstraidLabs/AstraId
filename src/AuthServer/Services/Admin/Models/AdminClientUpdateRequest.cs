using AuthServer.Models;

namespace AuthServer.Services.Admin.Models;

public sealed class AdminClientUpdateRequest
{
    public string? ClientId { get; set; }
    public string? DisplayName { get; set; }
    public string? ClientType { get; set; }
    public bool Enabled { get; set; } = true;
    public IReadOnlyList<string> GrantTypes { get; set; } = Array.Empty<string>();
    public bool PkceRequired { get; set; }
    public string? ClientApplicationType { get; set; }
    public bool AllowIntrospection { get; set; }
    public bool AllowUserCredentials { get; set; }
    public IReadOnlyList<string> AllowedScopesForPasswordGrant { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> RedirectUris { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> PostLogoutRedirectUris { get; set; } = Array.Empty<string>();
    public string? Profile { get; set; }
    public string? PresetId { get; set; }
    public int? PresetVersion { get; set; }
    public System.Text.Json.JsonElement? Overrides { get; set; }
    public bool ForceSystemManagedEdit { get; set; }
    public ClientBranding? Branding { get; set; }
}
