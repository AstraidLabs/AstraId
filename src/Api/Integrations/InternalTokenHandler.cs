using System.IdentityModel.Tokens.Jwt;
using Api.Security;
using AstraId.Logging.Audit;
using Microsoft.AspNetCore.Authorization;

namespace Api.Integrations;

/// <summary>
/// Provides internal token handler functionality.
/// </summary>
public sealed class InternalTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IInternalTokenService _internalTokenService;
    private readonly ILogger<InternalTokenHandler> _logger;
    private readonly ISecurityAuditLogger _securityAuditLogger;
    private readonly IWebHostEnvironment _environment;

    public InternalTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        IInternalTokenService internalTokenService,
        ILogger<InternalTokenHandler> logger,
        ISecurityAuditLogger securityAuditLogger,
        IWebHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _internalTokenService = internalTokenService;
        _logger = logger;
        _securityAuditLogger = securityAuditLogger;
        _environment = environment;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("Authenticated user context is required for internal token forwarding.");
        }

        var grantedScopes = ResolveGrantedScopes(context).ToArray();
        var token = _internalTokenService.CreateToken(context.User, grantedScopes);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        _logger.LogInformation("Forwarding request to AppServer with internal token. Method: {Method}, Path: {Path}", request.Method, request.RequestUri?.AbsolutePath);
        _securityAuditLogger.Log(new SecurityAuditEvent
        {
            EventType = "api.internal_token.minted",
            Service = "Api",
            Environment = _environment.EnvironmentName,
            ActorType = "user",
            ActorId = context.User.FindFirst("sub")?.Value,
            Target = request.RequestUri?.AbsolutePath,
            Action = request.Method.Method,
            Result = "success",
            ReasonCode = "token_issued",
            CorrelationId = context.TraceIdentifier,
            TraceId = context.TraceIdentifier,
            Ip = context.Connection.RemoteIpAddress?.ToString(),
            UserAgentHash = null
        });
        _logger.LogInformation("Internal token metadata kid={Kid} jti={Jti}", jwt.Header.Kid, jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value);

        return base.SendAsync(request, cancellationToken);
    }

    private static IEnumerable<string> ResolveGrantedScopes(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var policies = endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Select(data => data.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        if (policies.Contains("RequireContentWrite", StringComparer.Ordinal))
        {
            return ["content.write"];
        }

        if (policies.Contains("RequireContentRead", StringComparer.Ordinal))
        {
            return ["content.read"];
        }

        return [];
    }
}
