using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services;

public static class ProblemDetailsExtensions
{
    public static ProblemDetails ApplyDefaults(this ProblemDetails details, HttpContext context)
    {
        ProblemDetailsDefaults.ApplyDefaults(details, context);
        return details;
    }

    public static ValidationProblemDetails ApplyDefaults(this ValidationProblemDetails details, HttpContext context)
    {
        ProblemDetailsDefaults.ApplyDefaults(details, context);
        return details;
    }
}
