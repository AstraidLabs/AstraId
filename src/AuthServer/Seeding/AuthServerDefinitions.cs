using Company.Auth.Contracts;
using OpenIddict.Abstractions;

namespace AuthServer.Seeding;

public static class AuthServerDefinitions
{
    public static readonly IReadOnlyCollection<PermissionDefinition> Permissions =
    [
        new PermissionDefinition
        {
            Key = "system.admin",
            Description = "System administration access.",
            Group = "System"
        }
    ];

    public static readonly IReadOnlyCollection<ApiResourceDefinition> ApiResources =
    [
        new ApiResourceDefinition
        {
            Name = "api",
            DisplayName = "Core API",
            Scopes = ["api"],
            Resource = "api"
        }
    ];

    public static readonly IReadOnlyCollection<ApiResourceSeedDefinition> ApiResourceSeeds =
    [
        new ApiResourceSeedDefinition(
            Name: "api",
            DisplayName: "Core API",
            BaseUrl: "https://localhost:7002",
            IsActive: true)
    ];

    public static readonly IReadOnlyCollection<OAuthClientDefinition> Clients =
    [
        new OAuthClientDefinition
        {
            ClientId = "web-spa",
            DisplayName = "Web SPA",
            Type = OpenIddictConstants.ClientTypes.Public,
            RedirectUris = [new Uri("http://localhost:5173/auth/callback")],
            PostLogoutRedirectUris = [new Uri("http://localhost:5173/")],
            Scopes =
            [
                AuthConstants.Scopes.OpenId,
                AuthConstants.Scopes.Profile,
                AuthConstants.Scopes.Email,
                AuthConstants.Scopes.OfflineAccess,
                "api"
            ],
            AllowedGrantTypes =
            [
                OpenIddictConstants.GrantTypes.AuthorizationCode,
                OpenIddictConstants.GrantTypes.RefreshToken
            ]
        }
    ];
}
