using AuthServer.Authorization;
using AuthServer.Data;
using AuthServer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace AuthServer.Services.Diagnostics;

public sealed class ExceptionHandlingMiddleware
{
    private const int MaxStackTraceLength = 12_000;
    private const int MaxInnerExceptionLength = 2000;

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DiagnosticsOptions> _diagnosticsOptions;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<DiagnosticsOptions> diagnosticsOptions)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _diagnosticsOptions = diagnosticsOptions;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("The response has already started, skipping exception handling.");
                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var errorId = Guid.NewGuid();
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var statusCode = StatusCodes.Status500InternalServerError;

        _logger.LogError(
            exception,
            "Unhandled exception captured. ErrorId: {ErrorId}, TraceId: {TraceId}",
            errorId,
            traceId);

        await StoreErrorLogAsync(context, exception, errorId, traceId, statusCode);

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        if (RequestAccepts.WantsHtml(context.Request))
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(BuildHtmlErrorPage(errorId, traceId, statusCode));
            return;
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode
        }.ApplyDefaults(context);

        problemDetails.Extensions["errorId"] = errorId;

        if (_diagnosticsOptions.Value.ExposeToAdmins && AdminAccessEvaluator.IsAdminUser(context.User))
        {
            problemDetails.Extensions["debug"] = BuildDebugPayload(exception, context);
        }

        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private async Task StoreErrorLogAsync(
        HttpContext context,
        Exception exception,
        Guid errorId,
        string traceId,
        int statusCode)
    {
        var options = _diagnosticsOptions.Value;
        if (!options.StoreErrorLogs)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var actorUserId = TryResolveUserId(context.User);

            var error = new ErrorLog
            {
                Id = errorId,
                TimestampUtc = DateTime.UtcNow,
                TraceId = traceId,
                ActorUserId = actorUserId,
                Path = context.Request.Path.HasValue ? context.Request.Path.Value! : string.Empty,
                Method = context.Request.Method,
                StatusCode = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Detail = ProblemDetailsDefaults.GetDefaultDetail(statusCode) ?? string.Empty,
                ExceptionType = exception.GetType().FullName,
                StackTrace = Truncate(exception.ToString(), MaxStackTraceLength),
                InnerException = Truncate(exception.InnerException?.Message, MaxInnerExceptionLength),
                DataJson = SerializeExceptionData(exception),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                RemoteIp = context.Connection.RemoteIpAddress?.ToString()
            };

            dbContext.ErrorLogs.Add(error);
            await dbContext.SaveChangesAsync(context.RequestAborted);
        }
        catch (Exception logException)
        {
            _logger.LogError(logException, "Failed to store error log.");
        }
    }

    private static Guid? TryResolveUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }

    private static string BuildHtmlErrorPage(Guid errorId, string traceId, int statusCode)
    {
        var detail = ProblemDetailsDefaults.GetDefaultDetail(statusCode) ?? "An unexpected error occurred.";
        return $"""
           <!DOCTYPE html>
           <html lang="en">
           <head>
             <meta charset="utf-8" />
             <meta name="viewport" content="width=device-width, initial-scale=1" />
             <title>Something went wrong</title>
             <style>
               body {{font - family: "Segoe UI", system-ui, sans-serif; margin: 40px; color: #0f172a; }}
               .card {{max - width: 640px; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px; }}
               .meta {{margin - top: 16px; font-size: 0.9rem; color: #475569; }}
             </style>
           </head>
           <body>
             <div class="card">
               <h1>We hit a snag</h1>
               <p>{detail}</p>
               <p class="meta">Error ID: {errorId}<br/>Trace ID: {traceId}</p>
             </div>
           </body>
           </html>
           """;
    }

    private static object BuildDebugPayload(Exception exception, HttpContext context)
    {
        return new
        {
            exceptionType = exception.GetType().FullName,
            stackTrace = Truncate(exception.ToString(), MaxStackTraceLength),
            innerExceptionSummary = Truncate(exception.InnerException?.Message, MaxInnerExceptionLength),
            path = context.Request.Path.Value,
            method = context.Request.Method
        };
    }

    private static string? SerializeExceptionData(Exception exception)
    {
        if (exception.Data.Count == 0)
        {
            return null;
        }

        try
        {
            var data = exception.Data.Keys.Cast<object>()
                .ToDictionary(key => key.ToString() ?? string.Empty, key => exception.Data[key]?.ToString());
            return JsonSerializer.Serialize(data);
        }
        catch
        {
            return null;
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
