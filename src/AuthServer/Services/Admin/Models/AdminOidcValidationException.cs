using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services.Admin.Models;

public sealed class AdminOidcValidationException : Exception
{
    public AdminOidcValidationException(string title, params string[] errors)
        : base(title)
    {
        Title = title;
        Errors = errors.Length == 0 ? ["Validation failed."] : errors;
    }

    public string Title { get; }
    public IReadOnlyList<string> Errors { get; }

    public ValidationProblemDetails ToProblemDetails(string key)
    {
        var details = new ValidationProblemDetails
        {
            Title = Title,
            Detail = string.Join(" ", Errors)
        };

        details.Errors.Add(key, Errors.ToArray());
        return details;
    }
}
