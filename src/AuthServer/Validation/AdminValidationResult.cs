using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Validation;

public sealed class AdminValidationResult
{
    private readonly Dictionary<string, List<string>> _fieldErrors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _generalErrors = new();

    public IReadOnlyDictionary<string, List<string>> FieldErrors => _fieldErrors;
    public IReadOnlyList<string> GeneralErrors => _generalErrors;
    public bool IsValid => _fieldErrors.Count == 0 && _generalErrors.Count == 0;

    public void AddFieldError(string field, string message)
    {
        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (!_fieldErrors.TryGetValue(field, out var list))
        {
            list = new List<string>();
            _fieldErrors[field] = list;
        }

        list.Add(message);
    }

    public void AddGeneralError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _generalErrors.Add(message);
    }

    public ValidationProblemDetails ToProblemDetails(string title, string? detail = null)
    {
        var errors = _fieldErrors.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());
        var details = new ValidationProblemDetails(errors)
        {
            Title = title,
            Detail = detail ?? BuildDetail()
        };

        if (_generalErrors.Count > 0)
        {
            details.Extensions["generalErrors"] = _generalErrors.ToArray();
        }

        return details;
    }

    private string BuildDetail()
    {
        var messages = _fieldErrors.SelectMany(entry => entry.Value)
            .Concat(_generalErrors)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToList();

        return messages.Count == 0 ? string.Empty : string.Join(" ", messages);
    }
}
