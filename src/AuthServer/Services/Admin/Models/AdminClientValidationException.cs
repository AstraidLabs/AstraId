using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services.Admin.Models;

public sealed class AdminClientValidationException : Exception
{
    public AdminClientValidationException(params string[] errors)
        : base("Client validation failed.")
    {
        Errors = errors.Length == 0 ? ["Validation failed."] : errors;
    }

    public IReadOnlyList<string> Errors { get; }

    public ValidationProblemDetails ToProblemDetails()
    {
        var details = new ValidationProblemDetails
        {
            Title = "Invalid client configuration.",
            Detail = string.Join(" ", Errors)
        };

        details.Errors.Add("client", Errors.ToArray());
        return details;
    }
}
