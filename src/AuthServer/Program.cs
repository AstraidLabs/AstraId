using StackExchange.Redis;
using MediatR;
using Hangfire.InMemory;
using Hangfire;
using AuthServer.Services.Jobs;
using AuthServer.Services.Events;
using AuthServer.Application.Commands;
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
using AstraId.Logging.Audit;
using AstraId.Logging.Extensions;
using AstraId.Logging.Options;
using AstraId.Logging.Redaction;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore;

// Identity provider host: serves OpenID Connect endpoints, interactive auth UI, and key/token governance jobs.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAstraLogging(builder.Configuration, builder.Environment);

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

// Persistence store for ASP.NET Identity + OpenIddict artifacts; authority state must remain durable across restarts.
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

builder.Services.RemoveAll<IUserStore<ApplicationUser>>();
builder.Services.AddScoped<IUserStore<ApplicationUser>, ProtectedUserStore>();

// Interactive login cookie is intentionally restricted because it represents the browser user session with issuer authority.
builder.Services.ConfigureApplicationCookie(options =>
{
    // Keep the primary auth cookie browser-only and HTTPS-only because it carries interactive user session state.
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
if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Redis:ConnectionString must be configured outside Development.");
    }

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
    builder.Services.AddSingleton<IEventPublisher, RedisEventPublisher>();
}
builder.Services.AddScoped<SecurityMaintenanceJobs>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SendEmailCommand>());
builder.Services.AddHangfire(configuration => configuration.UseInMemoryStorage());
builder.Services.AddHangfireServer();
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
builder.Services.AddSingleton<OidcClientLintService>();
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
builder.Services.AddScoped<IOAuthAdvancedPolicyProvider, OAuthAdvancedPolicyProvider>();
builder.Services.AddSingleton<DataProtectionStatusService>();
builder.Services.AddSingleton<EncryptionKeyStatusService>();
builder.Services.AddSingleton<AuthRateLimiter>();
builder.Services.AddSingleton<MfaChallengeStore>();
builder.Services.AddScoped<UserSessionRevocationService>();
builder.Services.AddScoped<AuthServer.Services.Sessions.ClientSessionTracker>();
builder.Services.AddScoped<AuthServer.Services.Sessions.BackChannelLogoutService>();
builder.Services.AddScoped<TokenExchangeService>();
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
builder.Services.AddOptions<AuthServer.Options.CorsOptions>()
    // Binds Cors:* where AllowedOrigins is an explicit allow-list; wildcard origins are intentionally rejected below.
    .Bind(builder.Configuration.GetSection(AuthServer.Options.CorsOptions.SectionName))
    .Validate(options => !options.AllowedOrigins.Any(origin => origin.Trim() == "*"), "Cors:AllowedOrigins cannot contain '*'.")
    .Validate(options => !options.AllowCredentials || options.AllowedOrigins.Length > 0, "Cors:AllowedOrigins must not be empty when AllowCredentials is true.")
    .Validate(options => builder.Environment.IsDevelopment() || (!options.AllowCredentials || options.AllowedOrigins.All(origin => origin.Trim() != "*")), "Unsafe CORS configuration for production.")
    .ValidateOnStart();
// Security hardening options are validated on startup so unsafe production header/CSP config fails fast.
builder.Services.AddOptions<SecurityHeadersOptions>()
    .Bind(builder.Configuration.GetSection(SecurityHeadersOptions.SectionName))
    .Validate(options => !builder.Environment.IsProduction() || options.AllowedFrameAncestors.All(value => value.Trim() != "*"), "SecurityHeaders:AllowedFrameAncestors cannot contain '*' in production.")
    .ValidateOnStart();
builder.Services.AddOptions<SecurityHardeningOptions>()
    .Bind(builder.Configuration.GetSection(SecurityHardeningOptions.SectionName));
builder.Services.PostConfigure<SecurityHardeningOptions>(options =>
{
    if (builder.Environment.IsProduction())
    {
        options.Enabled = true;
        options.RateLimiting.Enabled = true;
        options.Headers.Enabled = true;
        options.Cors.StrictMode = true;
    }
});
builder.Services.AddSingleton<UiUrlBuilder>();
builder.Services.AddSingleton<ReturnUrlValidator>();
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .ValidateOnStart();
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection(NotificationOptions.SectionName));
builder.Services.Configure<BootstrapAdminOptions>(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services.Configure<AuthServerCertificateOptions>(builder.Configuration.GetSection(AuthServerCertificateOptions.SectionName));
builder.Services.Configure<AuthServerDataProtectionOptions>(builder.Configuration.GetSection(AuthServerDataProtectionOptions.SectionName));
builder.Services.Configure<KeyRotationDefaultsOptions>(builder.Configuration.GetSection(KeyRotationDefaultsOptions.SectionName));
builder.Services.Configure<TokenPolicyDefaultsOptions>(builder.Configuration.GetSection(TokenPolicyDefaultsOptions.SectionName));
builder.Services.Configure<GovernanceGuardrailsOptions>(builder.Configuration.GetSection(GovernanceGuardrailsOptions.SectionName));
builder.Services.Configure<AuthServerAuthFeaturesOptions>(builder.Configuration.GetSection(AuthServerAuthFeaturesOptions.SectionName));
builder.Services.Configure<AuthServerDeviceFlowOptions>(builder.Configuration.GetSection(AuthServerDeviceFlowOptions.SectionName));
builder.Services.Configure<TokenExchangeOptions>(builder.Configuration.GetSection(TokenExchangeOptions.SectionName));
builder.Services.Configure<SeededClientSecretsOptions>(builder.Configuration.GetSection(SeededClientSecretsOptions.SectionName));
builder.Services.Configure<SecurityDiagnosticsOptions>(builder.Configuration.GetSection(SecurityDiagnosticsOptions.SectionName));
builder.Services.Configure<SessionManagementOptions>(builder.Configuration.GetSection(SessionManagementOptions.SectionName));
builder.Services.AddOptions<OAuthAdvancedPolicyDefaultsOptions>()
    .Bind(builder.Configuration.GetSection(OAuthAdvancedPolicyDefaultsOptions.SectionName))
    .Validate(options => options.DeviceFlowPollingIntervalSeconds >= 5, "OAuth advanced defaults: device polling interval must be >= 5 seconds.")
    .Validate(options => options.DeviceFlowUserCodeTtlMinutes is >= 1 and <= 60, "OAuth advanced defaults: user code TTL must be between 1 and 60 minutes.")
    .Validate(options => options.LogoutTokenTtlMinutes is >= 1 and <= 60, "OAuth advanced defaults: logout token TTL must be between 1 and 60 minutes.")
    .ValidateOnStart();
builder.Services.AddHttpClient(nameof(AuthServer.Services.Sessions.BackChannelLogoutService));
builder.Services.AddOptions<AuthServerSigningKeyOptions>()
    // Binds AuthServer:SigningKeys where intervals are expressed in days/minutes and KeySize is RSA bits.
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
    // Binds AuthServer:Tokens where token lifetimes are minutes (access/id) and days (refresh).
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
        options.Provider = options.GetProviderOrDefault();
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

builder.Services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
builder.Services.AddTransient<SmtpEmailSender>();
builder.Services.AddHttpClient<SendGridEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.sendgrid.com/");
});
builder.Services.AddHttpClient<MailgunEmailSender>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
    client.BaseAddress = new Uri(options.Mailgun.BaseUrl);
});
builder.Services.AddHttpClient<PostmarkEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.postmarkapp.com/");
});
builder.Services.AddHttpClient<GraphEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://graph.microsoft.com/");
});
builder.Services.AddSingleton<EmailSenderFactory>();
builder.Services.AddSingleton<IEmailSender, DelegatingEmailSender>();

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
        var hardening = builder.Configuration.GetSection(SecurityHardeningOptions.SectionName).Get<SecurityHardeningOptions>() ?? new SecurityHardeningOptions();
        var corsOptions = builder.Configuration.GetSection(AuthServer.Options.CorsOptions.SectionName).Get<AuthServer.Options.CorsOptions>() ?? new AuthServer.Options.CorsOptions();
        var origins = corsOptions.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (hardening.Enabled && hardening.Cors.StrictMode && corsOptions.AllowCredentials && origins.Length == 0)
        {
            throw new InvalidOperationException("SecurityHardening:Cors:StrictMode requires explicit Cors:AllowedOrigins when credentials are enabled.");
        }

        policy.AllowAnyHeader().AllowAnyMethod();
        if (origins.Length > 0)
        {
            policy.WithOrigins(origins);
            if (corsOptions.AllowCredentials)
            {
                policy.AllowCredentials();
            }
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        var services = context.HttpContext.RequestServices;
        services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiter")
            .LogWarning("Rate limit rejected request for path {Path}.", context.HttpContext.Request.Path);
        services.GetRequiredService<ISecurityAuditLogger>().Log(new SecurityAuditEvent
        {
            EventType = "rate_limit.rejected",
            Service = "AuthServer",
            Environment = services.GetRequiredService<IWebHostEnvironment>().EnvironmentName,
            ActorType = context.HttpContext.User.Identity?.IsAuthenticated == true ? "user" : "system",
            ActorId = context.HttpContext.User.FindFirst("sub")?.Value,
            Target = context.HttpContext.Request.Path.Value,
            Action = context.HttpContext.Request.Method,
            Result = "failure",
            ReasonCode = "rate_limit",
            CorrelationId = context.HttpContext.TraceIdentifier,
            TraceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier,
            Ip = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgentHash = LogSanitizer.ComputeStableHash(context.HttpContext.Request.Headers.UserAgent.ToString())
        });
        return ValueTask.CompletedTask;
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var key = context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        if (path.StartsWith("/admin/api", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"admin:{key}", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, QueueLimit = 0, Window = TimeSpan.FromMinutes(1) });
        }

        if (path.StartsWith("/connect/token", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"token:{key}", _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, QueueLimit = 0, Window = TimeSpan.FromMinutes(1) });
        }

        if (path.StartsWith("/connect/device", StringComparison.OrdinalIgnoreCase))
        {
            var limit = Math.Max(5, builder.Configuration.GetValue<int?>("AuthServer:DeviceFlow:PollingRateLimitPerMinute") ?? 30);
            return RateLimitPartition.GetFixedWindowLimiter($"device:{key}", _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, QueueLimit = 0, Window = TimeSpan.FromMinutes(1) });
        }

        if (path.StartsWith("/connect/verify", StringComparison.OrdinalIgnoreCase))
        {
            var limit = Math.Max(3, builder.Configuration.GetValue<int?>("AuthServer:DeviceFlow:VerificationRateLimitPerMinute") ?? 10);
            return RateLimitPartition.GetFixedWindowLimiter($"verify:{key}", _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, QueueLimit = 0, Window = TimeSpan.FromMinutes(1) });
        }

        if (path.StartsWith("/connect/introspect", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/connect/revocation", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/auth/login/mfa", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/auth/forgot-password", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/auth/reset-password", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"auth:{key}", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, QueueLimit = 0, Window = TimeSpan.FromMinutes(1) });
        }

        return RateLimitPartition.GetNoLimiter("default");
    });
});

// OpenIddict config defines issuer endpoints consumed by OIDC clients and resource servers.
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
        var authFeatures = builder.Configuration
            .GetSection(AuthServerAuthFeaturesOptions.SectionName)
            .Get<AuthServerAuthFeaturesOptions>() ?? new AuthServerAuthFeaturesOptions();
        // Issuer must be a stable public URI seen by clients and resource servers during token validation.
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

        // These OpenID Connect endpoints are called by clients/resource servers; discovery and JWKS are consumed automatically.
        options.SetConfigurationEndpointUris(".well-known/openid-configuration")
               .SetJsonWebKeySetEndpointUris(".well-known/jwks")
               .SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               .SetDeviceAuthorizationEndpointUris("connect/device")
               .SetEndUserVerificationEndpointUris("connect/verify")
               .SetIntrospectionEndpointUris("connect/introspect")
               .SetUserInfoEndpointUris("connect/userinfo")
               .SetEndSessionEndpointUris("connect/logout")
               .SetRevocationEndpointUris("connect/revocation");

        // Supported grant types are intentionally explicit to keep attack surface constrained to required client scenarios.
        options.AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow()
               .AllowClientCredentialsFlow();

        options.AllowDeviceAuthorizationFlow();
        options.AllowCustomFlow(TokenExchangeService.GrantType);

        if (authFeatures.EnablePasswordGrant)
        {
            options.AllowPasswordFlow();
        }

        options.RegisterScopes(AuthServerScopeRegistry.AllowedScopes.ToArray());

        // Access tokens are JWTs consumed by first-party resource servers, so signature validation is relied on instead of encryption.
        options.DisableAccessTokenEncryption();

        var certificateOptions = builder.Configuration
            .GetSection(AuthServerCertificateOptions.SectionName)
            .Get<AuthServerCertificateOptions>() ?? new AuthServerCertificateOptions();

        ConfigureEncryptionCertificates(options, certificateOptions, builder.Environment);

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableEndUserVerificationEndpointPassthrough()
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
        // Device authorization can be switched off at runtime without redeploying OpenIddict server configuration.
        options.AddEventHandler<OpenIddictServerEvents.ValidateDeviceAuthorizationRequestContext>(handler =>
        {
            handler.UseInlineHandler(async context =>
            {
                var provider = context.Transaction.GetHttpRequest()?.HttpContext.RequestServices.GetRequiredService<IOAuthAdvancedPolicyProvider>();
                if (provider is null)
                {
                    return;
                }

                var policy = await provider.GetCurrentAsync(context.CancellationToken);
                if (!policy.DeviceFlowEnabled)
                {
                    context.Reject(OpenIddictConstants.Errors.UnsupportedGrantType, "Device code flow is disabled.");
                }
            });
        });

        options.AddEventHandler<OpenIddictServerEvents.ValidateTokenRequestContext>(handler =>
        {
            handler.UseInlineHandler(async context =>
            {
                // Limit this handler to token exchange so other grant validations follow default OpenIddict behavior.
                if (!string.Equals(context.Request?.GrantType, TokenExchangeService.GrantType, StringComparison.Ordinal))
                {
                    return;
                }

                var provider = context.Transaction.GetHttpRequest()?.HttpContext.RequestServices.GetRequiredService<IOAuthAdvancedPolicyProvider>();
                if (provider is null)
                {
                    return;
                }

                var policy = await provider.GetCurrentAsync(context.CancellationToken);
                if (!policy.TokenExchangeEnabled)
                {
                    context.Reject(OpenIddictConstants.Errors.UnsupportedGrantType, "Token exchange is disabled.");
                }
            });
        });
    });

// Local validation lets this host authorize its own APIs using the same issuer metadata and signing credentials.
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddHostedService<AuthBootstrapHostedService>();
builder.Services.AddHostedService<OpenIddictClientSecretStorageStartupCheck>();
builder.Services.AddScoped<IOpenIddictClientSecretInspector, OpenIddictClientSecretInspector>();
builder.Services.AddHostedService<GovernancePolicyInitializer>();
builder.Services.AddHostedService<ErrorLogCleanupService>();
builder.Services.AddHostedService<SigningKeyRotationService>();
builder.Services.AddHostedService<InactivityLifecycleWorker>();
builder.Services.AddHostedService<RetentionCleanupService>();
builder.Services.AddHostedService<DeletionExecutorService>();
builder.Services.AddHostedService<EmailOutboxDispatcherService>();

var app = builder.Build();
var hardeningOptions = app.Services.GetRequiredService<IOptions<SecurityHardeningOptions>>().Value;

var authFeaturesOptions = app.Services.GetRequiredService<IOptions<AuthServerAuthFeaturesOptions>>().Value;
if (authFeaturesOptions.EnablePasswordGrant)
{
    app.Logger.LogWarning("Password grant enabled (legacy, not recommended).");
}


// Pipeline order keeps transport and browser hardening ahead of auth middleware and endpoint execution.
app.UseHttpsRedirection();
var securityHeadersOptions = app.Services.GetRequiredService<IOptions<SecurityHeadersOptions>>().Value;
if (app.Environment.IsProduction() && securityHeadersOptions.EnableHsts)
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    if (!hardeningOptions.Enabled || !hardeningOptions.Headers.Enabled || !securityHeadersOptions.Enabled)
    {
        await next();
        return;
    }

    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), fullscreen=()";
        context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-site";

        var frameAncestors = securityHeadersOptions.AllowedFrameAncestors.Length == 0
            ? "'none'"
            : string.Join(' ', securityHeadersOptions.AllowedFrameAncestors);
        var scriptSources = string.Join(' ', new[] { "'self'" }.Concat(securityHeadersOptions.AdditionalScriptSources ?? []));
        var csp = $"default-src 'self'; base-uri 'self'; frame-ancestors {frameAncestors}; object-src 'none'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src {scriptSources}; connect-src 'self';";

        if (securityHeadersOptions.CspMode == CspMode.Enforce)
        {
            context.Response.Headers["Content-Security-Policy"] = csp;
        }
        else if (securityHeadersOptions.CspMode == CspMode.ReportOnly)
        {
            context.Response.Headers["Content-Security-Policy-Report-Only"] = csp;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/connect/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/auth/", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
            context.Response.Headers.Pragma = "no-cache";
        }

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

app.UseAstraLogging();
// CORS policy only trusts configured origins because auth endpoints are credential-sensitive.
app.UseCors("Web");
app.UseAuthentication();
if (hardeningOptions.Enabled && hardeningOptions.RateLimiting.Enabled)
{
    app.UseRateLimiter();
}
app.UseMiddleware<UserActivityTrackingMiddleware>();
app.UseAuthorization();
app.UseHangfireDashboard("/admin/hangfire", new DashboardOptions
{
    Authorization = [new SecureDashboardAuthorizationFilter(app.Environment.IsDevelopment())]
});
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

// Liveness endpoint is anonymous by design so probes do not require OAuth credentials.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();
RecurringJob.AddOrUpdate<SecurityMaintenanceJobs>("auth-security-cleanup", job => job.CleanupAsync(), Cron.Daily);
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
    // Error persistence is best-effort diagnostics only; failures must never block the original status code response.
    var options = context.RequestServices.GetRequiredService<IOptions<DiagnosticsOptions>>().Value;
    if (!options.StoreErrorLogs)
    {
        return;
    }

    try
    {
        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var sanitizer = context.RequestServices.GetRequiredService<ILogSanitizer>();
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
            Path = sanitizer.SanitizePathAndQuery(context.Request.Path, context.Request.QueryString),
            Method = context.Request.Method,
            StatusCode = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = ProblemDetailsDefaults.GetDefaultDetail(statusCode) ?? string.Empty,
            UserAgent = sanitizer.SanitizeValue(context.Request.Headers.UserAgent.ToString()),
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
    // Defense-in-depth: encode rendered values so unexpected request-derived detail cannot become reflected XSS.
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
