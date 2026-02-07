using AuthServer.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AuthServer.Services;

public sealed class UiUrlBuilder
{
    private readonly AuthServerUiOptions _options;

    public UiUrlBuilder(IOptions<AuthServerUiOptions> options)
    {
        _options = options.Value;
    }

    public string BuildLoginUrl(string returnUrl)
    {
        return BuildUiUrl("/login", returnUrl);
    }

    public string BuildRegisterUrl(string returnUrl)
    {
        return BuildUiUrl("/register", returnUrl);
    }

    public string BuildHomeUrl()
    {
        return _options.IsHosted ? "/" : _options.UiBaseUrl.TrimEnd('/');
    }

    public string BuildActivationUrl(string email, string token)
    {
        return BuildUiUrl("/activate", new Dictionary<string, string?>
        {
            ["email"] = email,
            ["token"] = token
        });
    }

    public string BuildResetPasswordUrl(string email, string token)
    {
        return BuildUiUrl("/reset-password", new Dictionary<string, string?>
        {
            ["email"] = email,
            ["token"] = token
        });
    }

    public string BuildChangeEmailUrl(Guid userId, string email, string token, string? returnUrl)
    {
        var query = new Dictionary<string, string?>
        {
            ["userId"] = userId.ToString(),
            ["email"] = email,
            ["token"] = token
        };

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            query["returnUrl"] = returnUrl;
        }

        return BuildUiUrl("/account/email/confirm", query);
    }

    private string BuildUiUrl(string path, string returnUrl)
    {
        var baseUrl = _options.IsHosted ? string.Empty : _options.UiBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}{EnsureLeadingSlash(path)}";
        return QueryHelpers.AddQueryString(url, "returnUrl", returnUrl);
    }

    private string BuildUiUrl(string path, IReadOnlyDictionary<string, string?> queryParameters)
    {
        var baseUrl = _options.IsHosted ? string.Empty : _options.UiBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}{EnsureLeadingSlash(path)}";
        return QueryHelpers.AddQueryString(url, queryParameters);
    }

    private static string EnsureLeadingSlash(string path)
    {
        return path.StartsWith("/", StringComparison.Ordinal) ? path : $"/{path}";
    }
}
