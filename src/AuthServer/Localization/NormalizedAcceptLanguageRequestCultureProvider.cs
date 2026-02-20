using Microsoft.AspNetCore.Localization;
using Microsoft.Net.Http.Headers;

namespace AuthServer.Localization;

/// <summary>
/// Provides normalized accept language request culture provider functionality.
/// </summary>
public sealed class NormalizedAcceptLanguageRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var header = httpContext.Request.Headers[HeaderNames.AcceptLanguage].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return NullProviderCultureResult;
        }

        var selectedTag = ParseCandidates(header)
            .Select(candidate => LanguageTagNormalizer.Normalize(candidate))
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(selectedTag))
        {
            return NullProviderCultureResult;
        }

        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(selectedTag, selectedTag));
    }

    private static IEnumerable<string> ParseCandidates(string header)
    {
        var parts = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var weighted = new List<(string tag, decimal q, int index)>();

        for (var i = 0; i < parts.Length; i++)
        {
            var raw = parts[i];
            var tokenParts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokenParts.Length == 0)
            {
                continue;
            }

            var tag = tokenParts[0];
            if (string.IsNullOrWhiteSpace(tag) || tag == "*")
            {
                continue;
            }

            var q = 1m;
            var qPart = tokenParts.Skip(1).FirstOrDefault(part => part.StartsWith("q=", StringComparison.OrdinalIgnoreCase));
            if (qPart is not null && decimal.TryParse(qPart.AsSpan(2), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out var parsedQ))
            {
                q = parsedQ;
            }

            if (q <= 0)
            {
                continue;
            }

            weighted.Add((tag, q, i));
        }

        return weighted
            .OrderByDescending(entry => entry.q)
            .ThenBy(entry => entry.index)
            .Select(entry => entry.tag);
    }
}
