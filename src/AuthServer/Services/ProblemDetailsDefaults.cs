using System.Diagnostics;
using System.Globalization;
using AuthServer.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

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

    public static string? GetDefaultDetail(int statusCode, string? cultureTag = null)
    {
        var defaultDetail = DefaultDetails.TryGetValue(statusCode, out var detail) ? detail : null;
        if (defaultDetail is null)
        {
            return null;
        }

        var c = LanguageTagNormalizer.Normalize(cultureTag);
        return (statusCode, c) switch
        {
            (StatusCodes.Status400BadRequest, "cs-CZ") => "Zkontrolujte zadané údaje a zkuste to znovu.",
            (StatusCodes.Status400BadRequest, "sk-SK") => "Skontrolujte zadané údaje a skúste to znova.",
            (StatusCodes.Status400BadRequest, "pl-PL") => "Sprawdź wprowadzone dane i spróbuj ponownie.",
            (StatusCodes.Status400BadRequest, "de-DE") => "Bitte prüfen Sie die eingegebenen Daten und versuchen Sie es erneut.",
            (StatusCodes.Status422UnprocessableEntity, "cs-CZ") => "Některá pole jsou neplatná. Zkontrolujte je a zkuste to znovu.",
            (StatusCodes.Status422UnprocessableEntity, "sk-SK") => "Niektoré polia sú neplatné. Skontrolujte ich a skúste to znova.",
            (StatusCodes.Status422UnprocessableEntity, "pl-PL") => "Niektóre pola są nieprawidłowe. Sprawdź je i spróbuj ponownie.",
            (StatusCodes.Status422UnprocessableEntity, "de-DE") => "Einige Felder sind ungültig. Bitte prüfen und erneut versuchen.",
            (StatusCodes.Status429TooManyRequests, "cs-CZ") => "Příliš mnoho pokusů. Zkuste to prosím později.",
            (StatusCodes.Status429TooManyRequests, "sk-SK") => "Príliš veľa pokusov. Skúste to prosím neskôr.",
            (StatusCodes.Status429TooManyRequests, "pl-PL") => "Zbyt wiele prób. Spróbuj ponownie później.",
            (StatusCodes.Status429TooManyRequests, "de-DE") => "Zu viele Versuche. Bitte versuchen Sie es später erneut.",
            _ => defaultDetail
        };
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

        if (string.IsNullOrWhiteSpace(details.Detail))
        {
            details.Detail = GetDefaultDetail(statusCode, CultureInfo.CurrentUICulture.Name);
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            details.Extensions["traceId"] = traceId;
        }
    }
}
