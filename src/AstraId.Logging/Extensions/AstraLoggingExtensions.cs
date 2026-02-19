using AstraId.Logging.Audit;
using AstraId.Logging.Correlation;
using AstraId.Logging.Middleware;
using AstraId.Logging.Options;
using AstraId.Logging.Redaction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AstraId.Logging.Extensions;

public static class AstraLoggingExtensions
{
    public static IServiceCollection AddAstraLogging(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOptions<AstraLoggingOptions>()
            .Bind(configuration.GetSection(AstraLoggingOptions.SectionName));

        services.PostConfigure<AstraLoggingOptions>(options =>
        {
            options.Mode = string.IsNullOrWhiteSpace(options.Mode)
                ? (environment.IsDevelopment() ? "Development" : "Production")
                : options.Mode;

            if (!options.IsDevelopmentLike)
            {
                options.RedactionEnabled = true;
                options.RequestLogging.IncludeBody = false;
                options.RequestLogging.IncludeQueryString = false;
            }
        });

        services.AddSingleton<ILogSanitizer, LogSanitizer>();
        services.AddSingleton<ISecurityAuditLogger, SecurityAuditLogger>();

        services.AddSingleton<IConfigureOptions<LoggerFilterOptions>, AstraLoggerFilterConfigurator>();

        return services;
    }

    public static IApplicationBuilder UseAstraLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestCorrelationMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        return app;
    }
}

internal sealed class AstraLoggerFilterConfigurator : IConfigureOptions<LoggerFilterOptions>
{
    private readonly IOptions<AstraLoggingOptions> _options;

    public AstraLoggerFilterConfigurator(IOptions<AstraLoggingOptions> options)
    {
        _options = options;
    }

    public void Configure(LoggerFilterOptions options)
    {
        var value = _options.Value;
        options.AddFilter("AstraId.Logging.Application", value.Application.MinimumLevel);
        options.AddFilter("AstraId.Logging.SecurityAudit", value.SecurityAudit.MinimumLevel);

        if (value.DeveloperDiagnostics.Enabled)
        {
            options.AddFilter("AstraId.Logging.DeveloperDiagnostics", value.DeveloperDiagnostics.MinimumLevel);
        }
        else
        {
            options.AddFilter("AstraId.Logging.DeveloperDiagnostics", LogLevel.None);
        }
    }
}
