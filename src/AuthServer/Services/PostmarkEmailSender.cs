using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AuthServer.Options;
using Microsoft.Extensions.Options;

namespace AuthServer.Services;

public sealed class PostmarkEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<PostmarkEmailSender> _logger;

    public PostmarkEmailSender(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<PostmarkEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var payload = new
        {
            From = BuildFrom(options),
            To = message.ToEmail,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        };

        using var response = await SendWithRetryAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "email")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Postmark-Server-Token", options.Postmark.ServerToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return await _httpClient.SendAsync(request, cancellationToken);
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Email provider {Provider} returned non-success status {StatusCode}.", "Postmark", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Email sent via provider {Provider}. StatusCode: {StatusCode}. RecipientCount: {RecipientCount}", "Postmark", (int)response.StatusCode, 1);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> operation, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            response?.Dispose();
            response = await operation();
            if (!IsTransientFailure(response.StatusCode) || attempt == 3) return response;
            _logger.LogWarning("Email provider {Provider} transient failure ({StatusCode}) on attempt {Attempt}.", "Postmark", (int)response.StatusCode, attempt);
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), cancellationToken);
        }

        throw new InvalidOperationException("Email send failed due to unexpected response state.");
    }

    private static bool IsTransientFailure(HttpStatusCode statusCode) => statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;

    private static string BuildFrom(EmailOptions options) => string.IsNullOrWhiteSpace(options.FromName) ? options.FromEmail : $"{options.FromName} <{options.FromEmail}>";
}
