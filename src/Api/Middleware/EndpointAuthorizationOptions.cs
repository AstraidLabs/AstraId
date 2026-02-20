namespace Api.Middleware;

/// <summary>
/// Provides configuration options for endpoint authorization.
/// </summary>
public sealed class EndpointAuthorizationOptions
{
    public string RequiredScope { get; set; } = "api";
}
