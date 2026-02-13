using System.Security.Claims;
using Company.Auth.Contracts;

namespace ContentServer.Security;

public interface ICurrentUser
{
    string Subject { get; }
    string? Tenant { get; }
    IReadOnlyCollection<string> Permissions { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Subject => _httpContextAccessor.HttpContext?.User.FindFirstValue("sub") ?? string.Empty;
    public string? Tenant => _httpContextAccessor.HttpContext?.User.FindFirst("tenant")?.Value;

    public IReadOnlyCollection<string> Permissions =>
        _httpContextAccessor.HttpContext?.User
            .FindAll(AuthConstants.ClaimTypes.Permission)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
}
