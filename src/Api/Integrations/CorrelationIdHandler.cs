using Microsoft.Extensions.Primitives;

namespace Api.Integrations;

/// <summary>
/// Provides correlation id handler functionality.
/// </summary>
public sealed class CorrelationIdHandler : DelegatingHandler
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(HeaderName))
        {
            var correlationId = GetCorrelationId();
            request.Headers.Add(HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private string GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Request.Headers.TryGetValue(HeaderName, out StringValues values) == true
            && !StringValues.IsNullOrEmpty(values))
        {
            return values.ToString();
        }

        return Guid.NewGuid().ToString("D");
    }
}
