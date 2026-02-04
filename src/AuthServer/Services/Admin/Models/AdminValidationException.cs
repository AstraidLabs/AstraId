using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services.Admin.Models;

public sealed class AdminValidationException : Exception
{
    public AdminValidationException(string title, IReadOnlyDictionary<string, string[]> errors, string? detail = null)
        : base(title)
    {
        Title = title;
        Errors = errors;
        Detail = detail ?? BuildDetail(errors);
    }

    public string Title { get; }
    public string Detail { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationProblemDetails ToProblemDetails()
    {
        var details = new ValidationProblemDetails
        {
            Title = Title,
            Detail = Detail
        };

        foreach (var (key, messages) in Errors)
        {
            details.Errors.Add(key, messages);
        }

        return details;
    }

    private static string BuildDetail(IReadOnlyDictionary<string, string[]> errors)
    {
        return string.Join(" ", errors.SelectMany(item => item.Value).Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
