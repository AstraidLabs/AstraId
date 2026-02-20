using System.Net.Http.Json;

namespace Api.Integrations;

/// <summary>
/// Provides app server client functionality.
/// </summary>
public sealed class AppServerClient
{
    private readonly HttpClient _httpClient;

    public AppServerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Requests the full item list from the app-server service.
    /// </summary>
    public Task<HttpResponseMessage> GetItemsAsync(CancellationToken cancellationToken) =>
        _httpClient.GetAsync("app/items", cancellationToken);

    /// <summary>
    /// Creates a new app-server item using the provided payload.
    /// </summary>
    public Task<HttpResponseMessage> CreateItemAsync(object payload, CancellationToken cancellationToken) =>
        _httpClient.PostAsJsonAsync("app/items", payload, cancellationToken);
}
