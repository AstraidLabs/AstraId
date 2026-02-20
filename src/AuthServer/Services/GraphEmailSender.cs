using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuthServer.Options;
using Microsoft.Extensions.Options;

namespace AuthServer.Services;

/// <summary>
/// Provides graph email sender functionality.
/// </summary>
public sealed class GraphEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<GraphEmailSender> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public GraphEmailSender(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<GraphEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var token = await GetAccessTokenAsync(cancellationToken);
        var payload = new
        {
            message = new
            {
                subject = message.Subject,
                body = new
                {
                    contentType = string.IsNullOrWhiteSpace(message.HtmlBody) ? "Text" : "HTML",
                    content = string.IsNullOrWhiteSpace(message.HtmlBody) ? message.TextBody : message.HtmlBody
                },
                toRecipients = new[] { new { emailAddress = new { address = message.ToEmail, name = message.ToName } } }
            },
            saveToSentItems = false
        };

        using var response = await SendWithRetryAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"v1.0/users/{Uri.EscapeDataString(options.Graph.FromUser)}/sendMail")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _httpClient.SendAsync(request, cancellationToken);
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Email provider {Provider} returned non-success status {StatusCode}.", "Graph", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Email sent via provider {Provider}. StatusCode: {StatusCode}. RecipientCount: {RecipientCount}", "Graph", (int)response.StatusCode, 1);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
            {
                return _accessToken;
            }

            var options = _options.Value;
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{options.Graph.TenantId}/oauth2/v2.0/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = options.Graph.ClientId,
                    ["client_secret"] = options.Graph.ClientSecret,
                    ["scope"] = "https://graph.microsoft.com/.default"
                })
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Email provider {Provider} token request failed with status {StatusCode}.", "Graph", (int)response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            var tokenResponse = JsonSerializer.Deserialize<GraphTokenResponse>(await response.Content.ReadAsStringAsync(cancellationToken), new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("Graph token response was empty.");
            if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Graph token response did not contain an access token.");
            }

            _accessToken = tokenResponse.AccessToken;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, tokenResponse.ExpiresIn - 60));
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> operation, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            response?.Dispose();
            response = await operation();
            if (!IsTransientFailure(response.StatusCode) || attempt == 3) return response;
            _logger.LogWarning("Email provider {Provider} transient failure ({StatusCode}) on attempt {Attempt}.", "Graph", (int)response.StatusCode, attempt);
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), cancellationToken);
        }

        throw new InvalidOperationException("Email send failed due to unexpected response state.");
    }

    private static bool IsTransientFailure(HttpStatusCode statusCode) => statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;

    /// <summary>
    /// Provides graph token response functionality.
    /// </summary>
    private sealed record GraphTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
