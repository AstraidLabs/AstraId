using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services;

public static class ProblemDetailsDefaults
{
    public static readonly IReadOnlyDictionary<int, string> DefaultDetails = new Dictionary<int, string>
    {
        [StatusCodes.Status400BadRequest] = "Please check the entered data and try again.",
        [StatusCodes.Status401Unauthorized] = "Your session has expired. Please sign in again.",
        [StatusCodes.Status403Forbidden] = "You don’t have permission to access this page.",
        [StatusCodes.Status404NotFound] = "The requested resource was not found.",
        [StatusCodes.Status409Conflict] = "This action conflicts with an existing record.",
        [StatusCodes.Status422UnprocessableEntity] = "Some fields are invalid. Please review and try again.",
        [StatusCodes.Status429TooManyRequests] = "Too many attempts. Please try again later.",
        [StatusCodes.Status500InternalServerError] = "Something went wrong on our side. Please try again.",
        [StatusCodes.Status503ServiceUnavailable] = "The service is temporarily unavailable. Please try again later."
    };

    public static string? GetDefaultDetail(int statusCode)
    {
        return DefaultDetails.TryGetValue(statusCode, out var detail) ? detail : null;
    }

    public static void ApplyDefaults(ProblemDetails details, HttpContext context)
    {
        if (details is null)
        {
            throw new ArgumentNullException(nameof(details));
        }

        var statusCode = details.Status ?? context.Response.StatusCode;
        details.Status = statusCode;

        if (string.IsNullOrWhiteSpace(details.Title))
        {
            details.Title = ReasonPhrases.GetReasonPhrase(statusCode);
        }

        if (string.IsNullOrWhiteSpace(details.Detail) && DefaultDetails.TryGetValue(statusCode, out var detail))
        {
            details.Detail = detail;
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            details.Extensions["traceId"] = traceId;
        }
    }
}
