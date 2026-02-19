using System.Diagnostics;
using AstraId.Logging.Options;
using AstraId.Logging.Redaction;
using Microsoft.Extensions.Options;

namespace AstraId.Logging.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _applicationLogger;
    private readonly ILogger _developerLogger;
    private readonly ILogSanitizer _sanitizer;
    private readonly IOptions<AstraLoggingOptions> _options;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILoggerFactory loggerFactory,
        ILogSanitizer sanitizer,
        IOptions<AstraLoggingOptions> options)
    {
        _next = next;
        _applicationLogger = loggerFactory.CreateLogger("AstraId.Logging.Application");
        _developerLogger = loggerFactory.CreateLogger("AstraId.Logging.DeveloperDiagnostics");
        _sanitizer = sanitizer;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var opts = _options.Value;
        if (!opts.RequestLogging.Enabled)
        {
            await _next(context);
            return;
        }

        var path = opts.RequestLogging.IncludeQueryString
            ? _sanitizer.SanitizePathAndQuery(context.Request.Path, context.Request.QueryString)
            : context.Request.Path.Value ?? string.Empty;

        _applicationLogger.Log(opts.Application.MinimumLevel,
            "HTTP {Method} {Path} correlationId={CorrelationId} traceId={TraceId}",
            context.Request.Method,
            path,
            context.TraceIdentifier,
            Activity.Current?.Id ?? context.TraceIdentifier);

        if (opts.DeveloperDiagnostics.Enabled)
        {
            var headers = _sanitizer.SanitizeHeaders(context.Request.Headers);
            string? body = null;
            if (opts.RequestLogging.IncludeBody && IsBodyPathAllowed(context.Request.Path, opts.RequestLogging))
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                body = await reader.ReadToEndAsync(context.RequestAborted);
                context.Request.Body.Position = 0;
                if (body.Length > opts.RequestLogging.MaxBodyLength)
                {
                    body = body[..opts.RequestLogging.MaxBodyLength];
                }

                body = _sanitizer.SanitizeValue(body);
            }

            _developerLogger.Log(opts.DeveloperDiagnostics.MinimumLevel,
                "HTTP_DIAGNOSTIC method={Method} path={Path} query={Query} headers={Headers} body={Body} correlationId={CorrelationId} traceId={TraceId}",
                context.Request.Method,
                context.Request.Path.Value,
                opts.RequestLogging.IncludeQueryString ? _sanitizer.SanitizeQueryString(context.Request.Query) : string.Empty,
                headers,
                body,
                context.TraceIdentifier,
                Activity.Current?.Id ?? context.TraceIdentifier);
        }

        await _next(context);
    }

    private static bool IsBodyPathAllowed(PathString path, AstraLoggingOptions.RequestLoggingOptions options)
    {
        var pathValue = path.Value ?? string.Empty;
        if (options.BodyPathBlockListPrefixes.Any(prefix => pathValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return options.SafeBodyPathAllowList.Any(prefix => pathValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
