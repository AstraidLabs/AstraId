using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AstraId.Logging.Redaction;

/// <summary>
/// Provides log sanitizer functionality.
/// </summary>
public sealed class LogSanitizer : ILogSanitizer
{
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Cookie", "Set-Cookie", "X-Api-Key", "X-Internal-Api-Key", "X-Internal-Jwks-Api-Key"
    };

    private static readonly HashSet<string> SensitiveQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "token", "code", "password", "client_secret", "refresh_token", "access_token", "id_token", "assertion", "x-api-key", "jwks_internal_api_key", "legacyhs256secret"
    };

    private static readonly Regex SensitiveInlinePairRegex = new("(?i)(authorization|bearer|client_secret|password|refresh_token|access_token|id_token|api[_-]?key|token|cookie|jwks[_-]?internal[_-]?api[_-]?key|legacyhs256secret)\\s*[:=]\\s*([^\\s,;]+)", RegexOptions.Compiled);

    public string? SanitizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return SensitiveInlinePairRegex.Replace(value, "$1=[REDACTED]");
    }

    public string SanitizePathAndQuery(PathString path, QueryString query)
    {
        if (!query.HasValue)
        {
            return path.HasValue ? path.Value! : string.Empty;
        }

        var parsed = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(query.Value!);
        var sanitized = parsed
            .Select(pair => SensitiveQueryKeys.Contains(pair.Key)
                ? $"{Uri.EscapeDataString(pair.Key)}=[REDACTED]"
                : $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(SanitizeValue(pair.Value.ToString()) ?? string.Empty)}");

        return $"{path.Value}?{string.Join('&', sanitized)}";
    }

    public IDictionary<string, string> SanitizeHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            result[header.Key] = SensitiveHeaders.Contains(header.Key)
                ? "[REDACTED]"
                : (SanitizeValue(header.Value.ToString()) ?? string.Empty);
        }

        return result;
    }

    public string SanitizeQueryString(IQueryCollection query)
    {
        if (query.Count == 0)
        {
            return string.Empty;
        }

        return string.Join('&', query.Select(pair => SensitiveQueryKeys.Contains(pair.Key)
            ? $"{pair.Key}=[REDACTED]"
            : $"{pair.Key}={SanitizeValue(pair.Value.ToString())}"));
    }

    public static string ComputeStableHash(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
