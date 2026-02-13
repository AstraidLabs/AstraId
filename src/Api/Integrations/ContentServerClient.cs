using System.Net.Http.Json;

namespace Api.Integrations;

public sealed class ContentServerClient
{
    private readonly HttpClient _httpClient;

    public ContentServerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> GetItemsAsync(CancellationToken cancellationToken) =>
        _httpClient.GetAsync("content/items", cancellationToken);

    public Task<HttpResponseMessage> CreateItemAsync(object payload, CancellationToken cancellationToken) =>
        _httpClient.PostAsJsonAsync("content/items", payload, cancellationToken);
}
