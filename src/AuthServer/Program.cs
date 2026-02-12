using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using AuthServer.Authorization;
using AuthServer.Data;
using AuthServer.Options;
using AuthServer.Localization;
using AuthServer.Seeding;
using AuthServer.Services;
using AuthServer.Services.Diagnostics;
using AuthServer.Services.Cors;
using AuthServer.Services.Cryptography;
using AuthServer.Services.Admin;
using AuthServer.Services.SigningKeys;
using AuthServer.Services.Tokens;
using AuthServer.Services.Governance;
using AuthServer.Services.Security;
using AuthServer.Services.Notifications;
using AuthServer.Services.OpenIddict;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "AstraId.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<AuthMessages>>();
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = localizer["Common.Validation.InvalidRequest", "One or more validation errors occurred."],
                Detail = localizer["Common.Validation.CheckInput", "Please check the submitted values and try again."]
            }.ApplyDefaults(context.HttpContext);

            return new UnprocessableEntityObjectResult(problemDetails);
        };
    });
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(SupportedCultures.Default);
    options.SupportedCultures = SupportedCultures.All;
    options.SupportedUICultures = SupportedCultures.All;

    var providers = new List<IRequestCultureProvider>
    {
        new UserPreferredLanguageRequestCultureProvider()
    };

    if (builder.Environment.IsDevelopment())
    {
        providers.Add(new QueryStringRequestCultureProvider());
    }

    providers.Add(new NormalizedAcceptLanguageRequestCultureProvider());

    options.RequestCultureProviders = providers;
});

builder.Services.AddMemoryCache();
var dataProtectionBuilder = builder.Services.AddDataProtection();

builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminRoleService, AdminRoleService>();
builder.Services.AddScoped<IAdminPermissionAdminService, AdminPermissionAdminService>();
builder.Services.AddScoped<IAdminApiResourceService, AdminApiResourceService>();
builder.Services.AddScoped<IAdminEndpointService, AdminEndpointService>();
builder.Services.AddScoped<IAdminClientService, AdminClientService>();
builder.Services.AddSingleton<IClientProfileRegistry, ClientProfileRegistry>();
builder.Services.AddSingleton<IClientPresetRegistry, ClientPresetRegistry>();
builder.Services.AddSingleton<ClientConfigComposer>();
builder.Services.AddSingleton<ClientConfigValidator>();
builder.Services.AddScoped<IAdminOidcScopeService, AdminOidcScopeService>();
builder.Services.AddScoped<IAdminOidcResourceService, AdminOidcResourceService>();
builder.Services.AddScoped<AdminSigningKeyService>();
builder.Services.AddScoped<AdminTokenPolicyService>();
builder.Services.AddScoped<SigningKeyRotationCoordinator>();
builder.Services.AddScoped<KeyRotationPolicyService>();
builder.Services.AddScoped<TokenIncidentService>();
builder.Services.AddScoped<TokenRevocationService>();
builder.Services.AddScoped<IClientStateService, ClientStateService>();
builder.Services.AddScoped<IOidcClientPolicyEnforcer, OidcClientPolicyEnforcer>();
builder.Services.AddScoped<SigningKeyRingService>();
builder.Services.AddScoped<SigningKeyJwksService>();
builder.Services.AddScoped<TokenPolicyService>();
builder.Services.AddScoped<RefreshTokenReuseDetectionService>();
builder.Services.AddScoped<RefreshTokenReuseRemediationService>();
builder.Services.AddSingleton<TokenPolicyApplier>();
builder.Services.AddSingleton<ISigningKeyProtector, SigningKeyProtector>();
builder.Services.AddSingleton<ISigningKeyRotationState, SigningKeyRotationState>();
builder.Services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>, OpenIddictSigningCredentialsConfigurator>();
builder.Services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>, OpenIddictTokenPolicyConfigurator>();
builder.Services.AddScoped<GovernancePolicyStore>();
builder.Services.AddSingleton<DataProtectionStatusService>();
builder.Services.AddSingleton<EncryptionKeyStatusService>();
builder.Services.AddSingleton<AuthRateLimiter>();
builder.Services.AddSingleton<MfaChallengeStore>();
builder.Services.AddScoped<UserSessionRevocationService>();
builder.Services.AddScoped<IUserSecurityEventLogger, UserSecurityEventLogger>();
builder.Services.AddScoped<UserLifecycleService>();
builder.Services.AddScoped<InactivityPolicyService>();
builder.Services.AddScoped<EmailOutboxService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<LoginHistoryService>();
builder.Services.AddScoped<PrivacyGovernanceService>();
builder.Services.AddSingleton<AdminUiManifestService>();
builder.Services.AddScoped<ICorsPolicyProvider, ClientCorsPolicyProvider>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireAssertion(context =>
        {
            var hasPermissionClaim = context.User.HasClaim(claim => claim.Type == AuthConstants.ClaimTypes.Permission);
            return !hasPermissionClaim || context.User.HasClaim(AuthConstants.ClaimTypes.Permission, AuthConstants.Permissions.SystemAdmin);
        });
    });
});

builder.Services.Configure<AuthServerUiOptions>(builder.Configuration.GetSection(AuthServerUiOptions.SectionName));
builder.Services.Configure<DiagnosticsOptions>(builder.Configuration.GetSection(DiagnosticsOptions.SectionName));
builder.Services.AddSingleton<UiUrlBuilder>();
builder.Services.AddSingleton<ReturnUrlValidator>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection(NotificationOptions.SectionName));
builder.Services.Configure<BootstrapAdminOptions>(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services.Configure<AuthServerCertificateOptions>(builder.Configuration.GetSection(AuthServerCertificateOptions.SectionName));
builder.Services.Configure<AuthServerDataProtectionOptions>(builder.Configuration.GetSection(AuthServerDataProtectionOptions.SectionName));
builder.Services.Configure<KeyRotationDefaultsOptions>(builder.Configuration.GetSection(KeyRotationDefaultsOptions.SectionName));
builder.Services.Configure<TokenPolicyDefaultsOptions>(builder.Configuration.GetSection(TokenPolicyDefaultsOptions.SectionName));
builder.Services.Configure<GovernanceGuardrailsOptions>(builder.Configuration.GetSection(GovernanceGuardrailsOptions.SectionName));
builder.Services.AddOptions<AuthServerSigningKeyOptions>()
    .Bind(builder.Configuration.GetSection(AuthServerSigningKeyOptions.SectionName))
    .Validate(options =>
    {
        return options.RotationIntervalDays > 0
               && options.PreviousKeyRetentionDays >= 0
               && options.CheckPeriodMinutes > 0
               && options.KeySize >= 2048
               && !string.IsNullOrWhiteSpace(options.Algorithm)
               && Enum.IsDefined(options.Mode);
    }, "Signing key options are invalid.")
    .ValidateOnStart();
builder.Services.AddOptions<AuthServerTokenOptions>()
    .Bind(builder.Configuration.GetSection(AuthServerTokenOptions.SectionName))
    .Validate(options =>
    {
        return options.Public.AccessTokenMinutes > 0
               && options.Public.IdentityTokenMinutes > 0
               && options.Public.RefreshTokenAbsoluteDays > 0
               && options.Confidential.AccessTokenMinutes > 0
               && options.Confidential.IdentityTokenMinutes > 0
               && options.Confidential.RefreshTokenAbsoluteDays > 0
               && options.RefreshPolicy.ReuseLeewaySeconds >= 0;
    }, "Token policy options are invalid.")
    .ValidateOnStart();

if (builder.Environment.IsDevelopment())
{
    builder.Services.PostConfigure<EmailOptions>(options =>
    {
        options.FromEmail = string.IsNullOrWhiteSpace(options.FromEmail)
            ? "no-reply@local.test"
            : options.FromEmail;
        options.FromName ??= "AstraId";
        options.Smtp.Host = string.IsNullOrWhiteSpace(options.Smtp.Host)
            ? "localhost"
            : options.Smtp.Host;
        options.Smtp.Port = options.Smtp.Port <= 0 ? 2525 : options.Smtp.Port;
        options.Smtp.TimeoutSeconds = options.Smtp.TimeoutSeconds <= 0 ? 10 : options.Smtp.TimeoutSeconds;
    });
}

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var dataProtectionOptions = builder.Configuration
    .GetSection(AuthServerDataProtectionOptions.SectionName)
    .Get<AuthServerDataProtectionOptions>() ?? new AuthServerDataProtectionOptions();
if (!string.IsNullOrWhiteSpace(dataProtectionOptions.KeyPath))
{
    dataProtectionBuilder
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionOptions.KeyPath));
    if (dataProtectionOptions.ReadOnly)
    {
        dataProtectionBuilder.DisableAutomaticKeyGeneration();
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
        // Pokud pou��v� Guid jako PK:
        // .ReplaceDefaultEntities<Guid>();
    })
    .AddServer(options =>
    {
        var issuer = builder.Configuration["AuthServer:Issuer"] ?? AuthConstants.DefaultIssuer;
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri))
        {
            throw new InvalidOperationException("AuthServer:Issuer must be a valid absolute URI.");
        }

        if (builder.Environment.IsProduction()
            && !string.Equals(issuerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("AuthServer:Issuer must use HTTPS in production.");
        }

        options.SetIssuer(issuerUri);

        options.SetConfigurationEndpointUris(".well-known/openid-configuration")
               .SetJsonWebKeySetEndpointUris(".well-known/jwks")
               .SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               .SetIntrospectionEndpointUris("connect/introspect")
               .SetUserInfoEndpointUris("connect/userinfo")
               .SetEndSessionEndpointUris("connect/logout")
               .SetRevocationEndpointUris("connect/revocation");

        options.AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow()
               .AllowClientCredentialsFlow()
               .AllowPasswordFlow();

        options.RegisterScopes(AuthServerScopeRegistry.AllowedScopes.ToArray());

        options.DisableAccessTokenEncryption();

        var certificateOptions = builder.Configuration
            .GetSection(AuthServerCertificateOptions.SectionName)
            .Get<AuthServerCertificateOptions>() ?? new AuthServerCertificateOptions();

        ConfigureEncryptionCertificates(options, certificateOptions, builder.Environment);

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();

        options.AddEventHandler<OpenIddictServerEvents.ValidateIntrospectionRequestContext>(builder =>
        {
            builder.UseInlineHandler(OpenIddictIntrospectionHandlers.ValidateIntrospectionClientAsync);
        });

        options.AddEventHandler<OpenIddictServerEvents.ValidateRevocationRequestContext>(builder =>
        {
            builder.UseInlineHandler(OpenIddictIntrospectionHandlers.ValidateRevocationClientAsync);
        });
    });

builder.Services.AddHostedService<AuthBootstrapHostedService>();
builder.Services.AddHostedService<GovernancePolicyInitializer>();
builder.Services.AddHostedService<ErrorLogCleanupService>();
builder.Services.AddHostedService<SigningKeyRotationService>();
builder.Services.AddHostedService<InactivityLifecycleWorker>();
builder.Services.AddHostedService<RetentionCleanupService>();
builder.Services.AddHostedService<DeletionExecutorService>();
builder.Services.AddHostedService<EmailOutboxDispatcherService>();

var app = builder.Build();

var emailOptions = app.Services.GetRequiredService<IOptions<EmailOptions>>().Value;
ValidateEmailOptions(emailOptions, app.Environment);

app.UseHttpsRedirection();
if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self';";
        return Task.CompletedTask;
    });

    await next();
});
app.UseRequestLocalization();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.ContentLanguage = CultureInfo.CurrentUICulture.Name;
        return Task.CompletedTask;
    });

    await next();
});
app.UseMiddleware<ExceptionHandlingMiddleware>();

var uiOptions = app.Services.GetRequiredService<IOptions<AuthServerUiOptions>>().Value;
if (uiOptions.IsHosted)
{
    var hostedPath = uiOptions.GetHostedUiPath(app.Environment.ContentRootPath);
    var fileProvider = new PhysicalFileProvider(hostedPath);

    var defaultFiles = new DefaultFilesOptions { FileProvider = fileProvider };
    defaultFiles.DefaultFileNames.Clear();
    defaultFiles.DefaultFileNames.Add("index.html");
    app.UseDefaultFiles(defaultFiles);
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
}
else
{
    app.UseStaticFiles();
}

var adminUiRoot = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "admin-ui");
if (!string.IsNullOrWhiteSpace(app.Environment.WebRootPath) && Directory.Exists(adminUiRoot))
{
    var adminFileProvider = new PhysicalFileProvider(adminUiRoot);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = adminFileProvider,
        RequestPath = "/admin"
    });
}
app.UseRouting();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors("Web");

app.UseAuthentication();
app.UseMiddleware<UserActivityTrackingMiddleware>();
app.UseAuthorization();
app.UseStatusCodePages(async statusCodeContext =>
{
    var context = statusCodeContext.HttpContext;
    if (context.Response.HasStarted)
    {
        return;
    }

    var statusCode = context.Response.StatusCode;
    if (statusCode < 400)
    {
        return;
    }

    if (RequestAccepts.WantsHtml(context.Request))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var detail = ProblemDetailsDefaults.GetDefaultDetail(statusCode) ?? "An error occurred.";
        var errorId = statusCode >= 500 ? Guid.NewGuid() : (Guid?)null;
        if (statusCode >= 500)
        {
            await StoreStatusCodeErrorAsync(context, statusCode, errorId.Value);
        }

        await context.Response.WriteAsync(BuildStatusCodeHtml(statusCode, detail, context.TraceIdentifier, errorId));
        return;
    }

    var problemDetails = new ProblemDetails
    {
        Status = statusCode
    }.ApplyDefaults(context);

    if (statusCode >= 500)
    {
        var errorId = Guid.NewGuid();
        problemDetails.Extensions["errorId"] = errorId;
        await StoreStatusCodeErrorAsync(context, statusCode, errorId);
    }

    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(problemDetails);
});

app.MapControllers();
app.MapRazorPages();

if (!string.IsNullOrWhiteSpace(app.Environment.WebRootPath) && Directory.Exists(adminUiRoot))
{
    var adminFileProvider = new PhysicalFileProvider(adminUiRoot);
    app.MapFallbackToFile("/admin/{*path:nonfile}", "index.html", new StaticFileOptions
    {
        FileProvider = adminFileProvider
    });
}

if (uiOptions.IsHosted)
{
    var hostedPath = uiOptions.GetHostedUiPath(app.Environment.ContentRootPath);
    var fileProvider = new PhysicalFileProvider(hostedPath);
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
}

app.Run();

static async Task StoreStatusCodeErrorAsync(HttpContext context, int statusCode, Guid errorId)
{
    var options = context.RequestServices.GetRequiredService<IOptions<DiagnosticsOptions>>().Value;
    if (!options.StoreErrorLogs)
    {
        return;
    }

    try
    {
        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var actorUserId = Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : (Guid?)null;

        dbContext.ErrorLogs.Add(new ErrorLog
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
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            RemoteIp = context.Connection.RemoteIpAddress?.ToString()
        });

        await dbContext.SaveChangesAsync(context.RequestAborted);
    }
    catch (Exception exception)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("StatusCodePages");
        logger.LogError(exception, "Failed to store error log for status code {StatusCode}.", statusCode);
    }
}

static string BuildStatusCodeHtml(int statusCode, string detail, string traceId, Guid? errorId)
{
    // Bonus: lehká hardening ochrana proti XSS, kdyby se do "detail" někdy dostalo něco z requestu.
    var safeDetail = System.Net.WebUtility.HtmlEncode(detail);
    var safeTraceId = System.Net.WebUtility.HtmlEncode(traceId);

    var errorText = errorId.HasValue
        ? $"Error ID: {errorId}<br/>Trace ID: {safeTraceId}"
        : $"Trace ID: {safeTraceId}";

    return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{{statusCode}} Error</title>
              <style>
                body { font-family: "Segoe UI", system-ui, sans-serif; margin: 40px; color: #0f172a; }
                .card { max-width: 640px; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px; }
                .meta { margin-top: 16px; font-size: 0.9rem; color: #475569; }
              </style>
            </head>
            <body>
              <div class="card">
                <h1>{{ReasonPhrases.GetReasonPhrase(statusCode)}}</h1>
                <p>{{safeDetail}}</p>
                <p class="meta">{{errorText}}</p>
              </div>
            </body>
            </html>
            """;
}

static void ConfigureEncryptionCertificates(
    OpenIddictServerBuilder options,
    AuthServerCertificateOptions certificateOptions,
    IWebHostEnvironment environment)
{
    var encryptionCertificate = CertificateLoader.TryLoadCertificate(certificateOptions.Encryption);

    if (encryptionCertificate is null)
    {
        if (environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate();
        }
        else
        {
            throw new InvalidOperationException(
                "Encryption certificate is required. Configure AuthServer:Certificates:Encryption.");
        }
    }
    else
    {
        options.AddEncryptionCertificate(encryptionCertificate);
    }
}

static void ValidateEmailOptions(EmailOptions options, IHostEnvironment environment)
{
    var missingFrom = string.IsNullOrWhiteSpace(options.FromEmail);
    var missingHost = string.IsNullOrWhiteSpace(options.Smtp.Host);
    var invalidPort = options.Smtp.Port <= 0;

    if (missingFrom || missingHost || invalidPort)
    {
        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Email configuration is missing. Set Email:FromEmail, Email:Smtp:Host, and Email:Smtp:Port.");
        }

        throw new InvalidOperationException(
            "Email configuration is missing. Provide Email settings or use Development defaults.");
    }
}
