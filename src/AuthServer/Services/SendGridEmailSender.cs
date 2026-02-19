using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AuthServer.Options;
using Microsoft.Extensions.Options;

namespace AuthServer.Services;

public sealed class SendGridEmailSender : IEmailSender
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private readonly HttpClient _httpClient;
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(
        HttpClient httpClient,
        IOptions<EmailOptions> options,
        ILogger<SendGridEmailSender> logger)
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
            from = new
            {
                email = options.FromEmail,
                name = options.FromName
            },
            personalizations = new[]
            {
                new
                {
                    to = new[]
                    {
                        new
                        {
                            email = message.ToEmail,
                            name = message.ToName
                        }
                    },
                    subject = message.Subject
                }
            },
            content = BuildContent(message)
        };

        var bodyJson = JsonSerializer.Serialize(payload);

        HttpResponseMessage? response = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            response?.Dispose();
            using var request = new HttpRequestMessage(HttpMethod.Post, "v3/mail/send")
            {
                Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.SendGrid.ApiKey);

            response = await _httpClient.SendAsync(request, cancellationToken);
            if (!IsTransientFailure(response.StatusCode) || attempt == MaxAttempts)
            {
                break;
            }

            _logger.LogWarning(
                "Email provider {Provider} transient failure ({StatusCode}) on attempt {Attempt}.",
                "SendGrid",
                (int)response.StatusCode,
                attempt);

            await Task.Delay(RetryDelay, cancellationToken);
        }

        if (response is null)
        {
            throw new InvalidOperationException("Email send failed due to unexpected response state.");
        }

        try
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Email provider {Provider} returned non-success status {StatusCode}.",
                    "SendGrid",
                    (int)response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            var messageId = response.Headers.TryGetValues("X-Message-Id", out var values)
                ? values.FirstOrDefault()
                : null;

            _logger.LogInformation(
                "Email sent via provider {Provider}. StatusCode: {StatusCode}. MessageId: {MessageId}",
                "SendGrid",
                (int)response.StatusCode,
                string.IsNullOrWhiteSpace(messageId) ? "n/a" : messageId);
        }
        finally
        {
            response.Dispose();
        }
    }

    private static bool IsTransientFailure(HttpStatusCode statusCode)
        => statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;

    private static object[] BuildContent(EmailMessage message)
    {
        var content = new List<object>();
        if (!string.IsNullOrWhiteSpace(message.TextBody))
        {
            content.Add(new { type = "text/plain", value = message.TextBody });
        }

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            content.Add(new { type = "text/html", value = message.HtmlBody });
        }

        return content.ToArray();
    }
}
