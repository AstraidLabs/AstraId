using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AuthServer.Options;
using Microsoft.Extensions.Options;

namespace AuthServer.Services;

public sealed class MailgunEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<MailgunEmailSender> _logger;

    public MailgunEmailSender(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<MailgunEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        using var response = await SendWithRetryAsync(async () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"v3/{options.Mailgun.Domain}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{options.Mailgun.ApiKey}")));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["from"] = BuildFrom(options),
                ["to"] = message.ToEmail,
                ["subject"] = message.Subject,
                ["text"] = message.TextBody ?? string.Empty,
                ["html"] = message.HtmlBody
            });
            return await _httpClient.SendAsync(request, cancellationToken);
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Email provider {Provider} returned non-success status {StatusCode}.", "Mailgun", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Email sent via provider {Provider}. StatusCode: {StatusCode}. RecipientCount: {RecipientCount}", "Mailgun", (int)response.StatusCode, 1);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> operation, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            response?.Dispose();
            response = await operation();
            if (!IsTransientFailure(response.StatusCode) || attempt == 3) return response;
            _logger.LogWarning("Email provider {Provider} transient failure ({StatusCode}) on attempt {Attempt}.", "Mailgun", (int)response.StatusCode, attempt);
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), cancellationToken);
        }

        throw new InvalidOperationException("Email send failed due to unexpected response state.");
    }

    private static bool IsTransientFailure(HttpStatusCode statusCode) => statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;

    private static string BuildFrom(EmailOptions options) => string.IsNullOrWhiteSpace(options.FromName) ? options.FromEmail : $"{options.FromName} <{options.FromEmail}>";
}
