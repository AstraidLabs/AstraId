using System.Globalization;

namespace AuthServer.Localization;

public static class SupportedCultures
{
    public const string Default = "en-US";

    public static readonly string[] AllTags =
    [
        "en-US",
        "cs-CZ",
        "sk-SK",
        "pl-PL",
        "de-DE"
    ];

    public static readonly IReadOnlyDictionary<string, string> NeutralMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "en-US",
        ["cs"] = "cs-CZ",
        ["sk"] = "sk-SK",
        ["pl"] = "pl-PL",
        ["de"] = "de-DE"
    };

    public static readonly CultureInfo[] All = AllTags
        .Select(tag => new CultureInfo(tag))
        .ToArray();
}
