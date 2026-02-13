namespace Api.Middleware;

public sealed class EndpointAuthorizationOptions
{
    public string RequiredScope { get; set; } = "api";
}
