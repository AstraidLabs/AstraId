namespace AuthServer.Services.Admin.Validation;

/// <summary>
/// Provides admin validation errors functionality.
/// </summary>
public sealed class AdminValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.OrdinalIgnoreCase);

    public bool HasErrors => _errors.Count > 0;

    public void Add(string field, string message)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            field = "general";
        }

        if (!_errors.TryGetValue(field, out var list))
        {
            list = new List<string>();
            _errors[field] = list;
        }

        list.Add(message);
    }

    public IReadOnlyDictionary<string, string[]> ToDictionary()
    {
        return _errors.ToDictionary(item => item.Key, item => item.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }
}
