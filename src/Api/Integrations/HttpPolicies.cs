using Api.Options;
using Polly;
using Polly.Extensions.Http;

namespace Api.Integrations;

public static class HttpPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(HttpOptions options)
    {
        var retryCount = Math.Max(0, options.RetryCount);
        if (retryCount == 0)
        {
            return Policy.NoOpAsync<HttpResponseMessage>();
        }

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * options.RetryBaseDelaySeconds));
    }
}
