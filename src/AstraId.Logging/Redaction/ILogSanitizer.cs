namespace AstraId.Logging.Redaction;

/// <summary>
/// Defines the contract for log sanitizer.
/// </summary>
public interface ILogSanitizer
{
    string? SanitizeValue(string? value);
    string SanitizePathAndQuery(PathString path, QueryString query);
    IDictionary<string, string> SanitizeHeaders(IHeaderDictionary headers);
    string SanitizeQueryString(IQueryCollection query);
}
