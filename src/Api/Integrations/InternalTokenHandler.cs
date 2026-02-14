using Api.Security;

namespace Api.Integrations;

public sealed class InternalTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IInternalTokenService _internalTokenService;
    private readonly ILogger<InternalTokenHandler> _logger;

    public InternalTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        IInternalTokenService internalTokenService,
        ILogger<InternalTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _internalTokenService = internalTokenService;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("Authenticated user context is required for internal token forwarding.");
        }

        var grantedScopes = context.Request.Method switch
        {
            "GET" => new[] { "content.read" },
            _ => new[] { "content.write" }
        };

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            _internalTokenService.CreateToken(context.User, grantedScopes));

        _logger.LogInformation("Forwarding request to AppServer with internal token. Method: {Method}, Path: {Path}", request.Method, request.RequestUri?.AbsolutePath);

        return base.SendAsync(request, cancellationToken);
    }
}
