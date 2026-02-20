namespace Api.Integrations;

/// <summary>
/// Provides integration ping result functionality.
/// </summary>
public sealed record IntegrationPingResult(
    string Service,
    int StatusCode,
    bool IsSuccess)
{
    public static IntegrationPingResult FromResponse(string service, HttpResponseMessage response)
    {
        return new IntegrationPingResult(service, (int)response.StatusCode, response.IsSuccessStatusCode);
    }
}
