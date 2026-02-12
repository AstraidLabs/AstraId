namespace Company.Auth.Contracts;

public sealed class OAuthClientDefinition
{
    public required string ClientId { get; init; }
    public required string DisplayName { get; init; }
    public required string Type { get; init; }
    public required IReadOnlyCollection<Uri> RedirectUris { get; init; }
    public required IReadOnlyCollection<Uri> PostLogoutRedirectUris { get; init; }
    public required IReadOnlyCollection<string> Scopes { get; init; }
    public required IReadOnlyCollection<string> AllowedGrantTypes { get; init; }
    public string? ClientSecret { get; init; }
    public bool AllowIntrospection { get; init; }
    public string ClientApplicationType { get; init; } = "web";
    public bool AllowUserCredentials { get; init; }
    public IReadOnlyCollection<string> AllowedScopesForPasswordGrant { get; init; } = Array.Empty<string>();
}

public sealed class ApiResourceDefinition
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyCollection<string> Scopes { get; init; }
    public required string Resource { get; init; }
}

public sealed class PermissionDefinition
{
    public required string Key { get; init; }
    public required string Description { get; init; }
    public required string Group { get; init; }
}
