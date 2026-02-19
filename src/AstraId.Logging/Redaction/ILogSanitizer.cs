namespace AstraId.Logging.Redaction;

public interface ILogSanitizer
{
    string? SanitizeValue(string? value);
    string SanitizePathAndQuery(PathString path, QueryString query);
    IDictionary<string, string> SanitizeHeaders(IHeaderDictionary headers);
    string SanitizeQueryString(IQueryCollection query);
}
