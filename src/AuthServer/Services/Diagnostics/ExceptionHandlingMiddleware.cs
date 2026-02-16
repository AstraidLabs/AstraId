using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthServer.Authorization;
using AuthServer.Data;
using AuthServer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Diagnostics;

public sealed class ExceptionHandlingMiddleware
{
    private const int MaxStackTraceLength = 12_000;
    private const int MaxInnerExceptionLength = 2000;
    private static readonly Regex SensitivePairRegex = new("(?i)(authorization|bearer|client_secret|password|refresh_token|access_token)\\s*[:=]\\s*([^\\s,;]+)", RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DiagnosticsOptions> _diagnosticsOptions;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<DiagnosticsOptions> diagnosticsOptions,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _diagnosticsOptions = diagnosticsOptions;
        _environment = environment;
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

        _logger.LogError(exception, "Unhandled exception captured. ErrorId: {ErrorId}, TraceId: {TraceId}", errorId, traceId);

        await StoreErrorLogAsync(context, exception, errorId, traceId, statusCode);

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        if (RequestAccepts.WantsHtml(context.Request))
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(BuildHtmlErrorPage(errorId, traceId, statusCode));
            return;
        }

        var problemDetails = new ProblemDetails { Status = statusCode }.ApplyDefaults(context);
        problemDetails.Extensions["errorId"] = errorId;

        if (_diagnosticsOptions.Value.ExposeToAdmins && AdminAccessEvaluator.IsAdminUser(context.User))
        {
            problemDetails.Extensions["debug"] = BuildDebugPayload(exception, context);
        }

        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private async Task StoreErrorLogAsync(HttpContext context, Exception exception, Guid errorId, string traceId, int statusCode)
    {
        var options = _diagnosticsOptions.Value;
        if (!options.StoreErrorLogs)
        {
            return;
        }

        var includeDetails = !_environment.IsProduction() || options.StoreDetailedExceptionDataInProduction;

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
                StackTrace = includeDetails ? Redact(Truncate(exception.ToString(), MaxStackTraceLength)) : "Exception details hidden in production.",
                InnerException = includeDetails ? Redact(Truncate(exception.InnerException?.Message, MaxInnerExceptionLength)) : null,
                DataJson = includeDetails ? Redact(SerializeExceptionData(exception)) : null,
                UserAgent = Redact(context.Request.Headers.UserAgent.ToString()),
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
        return $$"""
               <!DOCTYPE html>
               <html lang="en">
               <head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Something went wrong</title></head>
               <body><h1>We hit a snag</h1><p>{{detail}}</p><p>Error ID: {{errorId}}<br/>Trace ID: {{traceId}}</p></body>
               </html>
               """;
    }

    private static object BuildDebugPayload(Exception exception, HttpContext context) => new
    {
        exceptionType = exception.GetType().FullName,
        stackTrace = Redact(Truncate(exception.ToString(), MaxStackTraceLength)),
        innerExceptionSummary = Redact(Truncate(exception.InnerException?.Message, MaxInnerExceptionLength)),
        path = context.Request.Path.Value,
        method = context.Request.Method
    };

    private static string? SerializeExceptionData(Exception exception)
    {
        if (exception.Data.Count == 0)
        {
            return null;
        }

        var data = exception.Data.Keys.Cast<object>()
            .ToDictionary(key => key.ToString() ?? string.Empty, key => exception.Data[key]?.ToString());
        return JsonSerializer.Serialize(data);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return SensitivePairRegex.Replace(value, "$1=[REDACTED]");
    }
}
