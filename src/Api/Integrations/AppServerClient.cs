using System.Net.Http.Json;

namespace Api.Integrations;

public sealed class AppServerClient
{
    private readonly HttpClient _httpClient;

    public AppServerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> GetItemsAsync(CancellationToken cancellationToken) =>
        _httpClient.GetAsync("app/items", cancellationToken);

    public Task<HttpResponseMessage> CreateItemAsync(object payload, CancellationToken cancellationToken) =>
        _httpClient.PostAsJsonAsync("app/items", payload, cancellationToken);
}
