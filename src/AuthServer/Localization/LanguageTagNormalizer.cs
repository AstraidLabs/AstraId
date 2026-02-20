namespace AuthServer.Localization;

/// <summary>
/// Provides language tag normalizer functionality.
/// </summary>
public static class LanguageTagNormalizer
{
    public static string Normalize(string? input)
    {
        return TryNormalize(input, out var normalized)
            ? normalized
            : SupportedCultures.Default;
    }

    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = SupportedCultures.Default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var candidate = input.Trim();

        if (SupportedCultures.AllTags.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            normalized = SupportedCultures.AllTags.First(tag => string.Equals(tag, candidate, StringComparison.OrdinalIgnoreCase));
            return true;
        }

        var neutral = candidate.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];
        if (SupportedCultures.NeutralMap.TryGetValue(neutral, out var mapped))
        {
            normalized = mapped;
            return true;
        }

        return false;
    }
}
