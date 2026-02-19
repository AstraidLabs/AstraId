using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Api.Hubs;
using Api.Events;
using AstraId.Contracts;
using Api.Contracts;
using Api.HealthChecks;
using Api.Integrations;
using Api.Middleware;
using Api.Models;
using Api.Options;
using Api.Security;
using Api.Services;
using Company.Auth.Api.Scopes;
using Company.Auth.Api;
using Company.Auth.Contracts;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var configuredIssuer = builder.Configuration["Auth:Issuer"];
var configuredAudience = builder.Configuration["Auth:Audience"];
var configuredRequiredScope = builder.Configuration["Auth:RequiredScope"];
var configuredValidationModeRaw = builder.Configuration["Auth:ValidationMode"];
var configuredValidationMode = AuthValidationModeParser.Parse(configuredValidationModeRaw);
var introspectionClientId = builder.Configuration["Auth:Introspection:ClientId"];
var introspectionClientSecret = builder.Configuration["Auth:Introspection:ClientSecret"];
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
var enableSignalRBackplane = builder.Configuration.GetValue<bool>("Redis:EnableSignalRBackplane");

var internalTokensSection = builder.Configuration.GetSection(InternalTokenOptions.SectionName);
var internalTokenLifetime = internalTokensSection.GetValue<int?>("TokenTtlSeconds") ?? 120;

if (!builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(configuredIssuer))
    {
        throw new InvalidOperationException("Auth:Issuer must be configured outside Development.");
    }

    if (!Uri.TryCreate(configuredIssuer, UriKind.Absolute, out var issuerUri))
    {
        throw new InvalidOperationException("Auth:Issuer must be a valid absolute URI outside Development.");
    }

    if (!string.Equals(issuerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Auth:Issuer must use HTTPS outside Development.");
    }

    if (string.IsNullOrWhiteSpace(configuredAudience))
    {
        throw new InvalidOperationException("Auth:Audience must be configured outside Development.");
    }

    if (string.IsNullOrWhiteSpace(configuredRequiredScope))
    {
        throw new InvalidOperationException("Auth:RequiredScope must be configured outside Development.");
    }

    if (!string.IsNullOrWhiteSpace(configuredValidationModeRaw)
        && !AuthValidationModeParser.TryParse(configuredValidationModeRaw, out _))
    {
        throw new InvalidOperationException("Auth:ValidationMode must be one of: Jwt, Introspection, Hybrid.");
    }

    if ((configuredValidationMode is AuthValidationMode.Introspection or AuthValidationMode.Hybrid)
        && (string.IsNullOrWhiteSpace(introspectionClientId) || string.IsNullOrWhiteSpace(introspectionClientSecret)))
    {
        throw new InvalidOperationException("Auth:Introspection:ClientId and Auth:Introspection:ClientSecret must be configured when introspection is enabled outside Development.");
    }
}

if (internalTokenLifetime is < 60 or > 300)
{
    throw new InvalidOperationException("InternalTokens:TokenTtlSeconds must be between 60 and 300.");
}

var effectiveIssuer = string.IsNullOrWhiteSpace(configuredIssuer)
    ? AuthConstants.DefaultIssuer
    : configuredIssuer;
var effectiveAudience = string.IsNullOrWhiteSpace(configuredAudience)
    ? "api"
    : configuredAudience;
var effectiveRequiredScope = string.IsNullOrWhiteSpace(configuredRequiredScope)
    ? "api"
    : configuredRequiredScope;
var configuredScopes = builder.Configuration.GetSection("Auth:Scopes").Get<string[]>();
var effectiveScopes = (configuredScopes is { Length: > 0 }
    ? configuredScopes
    : ["api"])
    .Concat([effectiveRequiredScope])
    .Distinct(StringComparer.Ordinal)
    .ToArray();
var effectiveClockSkew = builder.Configuration.GetValue<int?>("Auth:ClockSkewSeconds") ?? 0;

// Compatibility contract with AuthServer:
// - Issuer: Auth:Issuer (must be absolute URI and HTTPS outside Development).
// - Audience/resource: Auth:Audience (must match AuthServer protected API resource, canonical 'api').
// - Required scope baseline: Auth:RequiredScope (defaults to AuthServerScopeRegistry.ApiScope = 'api').
// - Permission claim type: AuthConstants.ClaimTypes.Permission ('permission').

var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.NewConfig<UserProfileModel, MeDto>();

builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddCompanyAuth(builder.Configuration, effectiveAudience);
builder.Services.AddOptions<InternalTokenOptions>()
    .Bind(internalTokensSection)
    .PostConfigure(options =>
    {
        options.Issuer = string.IsNullOrWhiteSpace(options.Issuer) ? "astraid-api" : options.Issuer;
        options.Audience = string.IsNullOrWhiteSpace(options.Audience) ? "astraid-app" : options.Audience;
        options.TokenTtlSeconds = options.TokenTtlSeconds <= 0 ? 120 : options.TokenTtlSeconds;
    })
    .Validate(options => options.TokenTtlSeconds is >= 60 and <= 300, "Internal token TTL must be in range 60-300 seconds.")
    .Validate(options => options.Signing.Algorithm is "RS256" or "ES256", "Internal token algorithm must be RS256 or ES256.")
    .Validate(options => options.Signing.KeySize >= 2048, "Internal token RSA key size must be at least 2048.")
    .Validate(options => options.Signing.RotationIntervalDays > 0, "Internal token rotation interval must be greater than zero.")
    .Validate(options => options.Signing.PreviousKeyRetentionDays >= options.Signing.RotationIntervalDays, "Previous key retention must be greater than or equal to rotation interval.")
    .ValidateOnStart();
builder.Services.AddSingleton<InternalTokenKeyRingService>();
builder.Services.AddSingleton<InternalJwksService>();
builder.Services.AddHostedService<InternalTokenKeyRotationService>();
builder.Services.AddSingleton<IInternalTokenService, InternalTokenService>();
builder.Services.Configure<EndpointAuthorizationOptions>(options =>
{
    options.RequiredScope = effectiveRequiredScope;
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSystemAdmin", policy =>
        policy.RequirePermission("system.admin"));

    options.AddPolicy("RequireContentRead", policy =>
        policy.RequireAssertion(context => ScopeParser.GetScopes(context.User).Contains("content.read")));

    options.AddPolicy("RequireContentWrite", policy =>
        policy.RequireAssertion(context => ScopeParser.GetScopes(context.User).Contains("content.write")));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    if (string.IsNullOrWhiteSpace(redisConnectionString))
    {
        throw new InvalidOperationException("Redis:ConnectionString must be configured.");
    }

    return ConnectionMultiplexer.Connect(redisConnectionString);
});

builder.Services.AddHostedService<RedisEventSubscriber>();

var signalRBuilder = builder.Services.AddSignalR();
if (enableSignalRBackplane)
{
    if (string.IsNullOrWhiteSpace(redisConnectionString))
    {
        throw new InvalidOperationException("Redis:ConnectionString must be configured when Redis backplane is enabled.");
    }

    signalRBuilder.AddStackExchangeRedis(redisConnectionString);
}

builder.Services.PostConfigureAll<JwtBearerOptions>(options =>
{
    var prior = options.Events?.OnMessageReceived;
    options.Events ??= new JwtBearerEvents();
    options.Events.OnMessageReceived = async context =>
    {
        if (prior is not null)
        {
            await prior(context);
        }

        if (!string.IsNullOrWhiteSpace(context.Token))
        {
            return;
        }

        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/app"))
        {
            context.Token = accessToken;
        }
    };
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        if (!builder.Environment.IsDevelopment() && context.ProblemDetails.Status >= 500)
        {
            context.ProblemDetails.Detail = "An unexpected error occurred.";
        }
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var scopes = effectiveScopes;

    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(new Uri(effectiveIssuer), "connect/authorize"),
                TokenUrl = new Uri(new Uri(effectiveIssuer), "connect/token"),
                Scopes = scopes.ToDictionary(scope => scope, scope => scope)
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            scopes
        }
    });
});

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

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<PolicyMapOptions>(builder.Configuration.GetSection("Api:AuthServer"));
builder.Services.AddSingleton<PolicyMapClient>();
builder.Services.AddHostedService<PolicyMapRefreshService>();

builder.Services.AddHttpClient(PolicyMapClient.HttpClientName, (sp, client) =>
    {
        var httpOptions = sp.GetRequiredService<IOptions<HttpOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(httpOptions.TimeoutSeconds);
    })
    .AddPolicyHandler((sp, _) => HttpPolicies.CreateRetryPolicy(sp.GetRequiredService<IOptions<HttpOptions>>().Value));

builder.Services.Configure<ServiceClientOptions>(ServiceNames.AuthServer, builder.Configuration.GetSection("Services:AuthServer"));
builder.Services.Configure<ServiceClientOptions>(ServiceNames.Cms, builder.Configuration.GetSection("Services:Cms"));
builder.Services.Configure<ServiceClientOptions>(ServiceNames.AppServer, builder.Configuration.GetSection("Services:AppServer"));
builder.Services.Configure<AppServerOptions>(builder.Configuration.GetSection("AppServer"));
builder.Services.Configure<HttpOptions>(builder.Configuration.GetSection("Http"));

builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services.AddTransient<InternalTokenHandler>();

builder.Services.AddHttpClient<AuthServerClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptionsMonitor<ServiceClientOptions>>().Get(ServiceNames.AuthServer);
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        }

        var httpOptions = sp.GetRequiredService<IOptions<HttpOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(httpOptions.TimeoutSeconds);
    })
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler(sp =>
        new ApiKeyHandler(ServiceNames.AuthServer, sp.GetRequiredService<IOptionsMonitor<ServiceClientOptions>>()))
    .AddPolicyHandler((sp, _) => HttpPolicies.CreateRetryPolicy(sp.GetRequiredService<IOptions<HttpOptions>>().Value));

builder.Services.AddHttpClient<CmsClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptionsMonitor<ServiceClientOptions>>().Get(ServiceNames.Cms);
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        }

        var httpOptions = sp.GetRequiredService<IOptions<HttpOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(httpOptions.TimeoutSeconds);
    })
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler(sp =>
        new ApiKeyHandler(ServiceNames.Cms, sp.GetRequiredService<IOptionsMonitor<ServiceClientOptions>>()))
    .AddPolicyHandler((sp, _) => HttpPolicies.CreateRetryPolicy(sp.GetRequiredService<IOptions<HttpOptions>>().Value));

builder.Services.AddHttpClient<AppServerClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptionsMonitor<ServiceClientOptions>>().Get(ServiceNames.AppServer);
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        }

        var httpOptions = sp.GetRequiredService<IOptions<HttpOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(httpOptions.TimeoutSeconds);
    })
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("AppServerMtls");
        var options = sp.GetRequiredService<IOptions<AppServerOptions>>().Value;
        var handler = new HttpClientHandler();

        if (!options.Mtls.Enabled)
        {
            logger.LogInformation("AppServer outbound mTLS disabled.");
            return handler;
        }

        logger.LogInformation("AppServer outbound mTLS enabled. Server validation mode: {Mode}", options.Mtls.ServerCertificate.ValidationMode);
        if (string.Equals(options.Mtls.ClientCertificate.Source, "File", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(options.Mtls.ClientCertificate.Path))
        {
            var cert = string.IsNullOrWhiteSpace(options.Mtls.ClientCertificate.Password)
                ? new X509Certificate2(options.Mtls.ClientCertificate.Path)
                : new X509Certificate2(options.Mtls.ClientCertificate.Path, options.Mtls.ClientCertificate.Password);
            handler.ClientCertificates.Add(cert);
        }

        if (string.Equals(options.Mtls.ServerCertificate.ValidationMode, "PinThumbprint", StringComparison.OrdinalIgnoreCase)
            && options.Mtls.ServerCertificate.PinnedThumbprints.Length > 0)
        {
            var allowed = options.Mtls.ServerCertificate.PinnedThumbprints
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant())
                .ToHashSet(StringComparer.Ordinal);

            handler.ServerCertificateCustomValidationCallback = (_, cert, _, errors) =>
            {
                if (errors != SslPolicyErrors.None || cert is null)
                {
                    return false;
                }

                var thumbprint = cert.GetCertHashString().ToUpperInvariant();
                return allowed.Contains(thumbprint);
            };
        }

        return handler;
    })
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler<InternalTokenHandler>()
    .AddPolicyHandler((sp, _) => HttpPolicies.CreateRetryPolicy(sp.GetRequiredService<IOptions<HttpOptions>>().Value));

builder.Services.AddHttpClient("HealthCheck", (sp, client) =>
{
    var httpOptions = sp.GetRequiredService<IOptions<HttpOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(httpOptions.TimeoutSeconds);
});

var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(builder.Configuration["Services:AuthServer:BaseUrl"]))
{
    healthChecks.AddCheck<AuthServerHealthCheck>("authserver");
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Services:Cms:BaseUrl"]))
{
    healthChecks.AddCheck<CmsHealthCheck>("cms");
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Services:AppServer:BaseUrl"]))
{
    healthChecks.AddCheck<CmsHealthCheck>("appserver");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
        var hardening = builder.Configuration.GetSection(SecurityHardeningOptions.SectionName).Get<SecurityHardeningOptions>() ?? new SecurityHardeningOptions();
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            if (builder.Environment.IsDevelopment())
            {
                allowedOrigins = ["http://localhost:5173"];
            }
            else
            {
                throw new InvalidOperationException("Cors:AllowedOrigins must be configured outside Development.");
            }
        }

        if (hardening.Enabled && hardening.Cors.StrictMode && allowedOrigins.Any(origin => origin.Trim() == "*"))
        {
            throw new InvalidOperationException("SecurityHardening:Cors:StrictMode forbids wildcard origins.");
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var key = context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        if (path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/integrations", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"admin:{key}", _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, QueueLimit = 0, Window = TimeSpan.FromMinutes(1) });
        }

        if (path.StartsWith("/hubs/app", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"hub:{key}", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, QueueLimit = 0, Window = TimeSpan.FromMinutes(1) });
        }

        return RateLimitPartition.GetNoLimiter("default");
    });
});

var app = builder.Build();
var hardeningOptions = app.Services.GetRequiredService<IOptions<SecurityHardeningOptions>>().Value;

app.UseExceptionHandler();
app.UseStatusCodePages();

var enableSwagger = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:EnableInProduction");
var authScheme = configuredValidationMode switch
{
    AuthValidationMode.Jwt => CompanyAuthExtensions.JwtScheme,
    AuthValidationMode.Introspection => CompanyAuthExtensions.IntrospectionScheme,
    _ => CompanyAuthExtensions.HybridScheme
};
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var clientId = builder.Configuration["Swagger:OAuthClientId"] ?? "web-spa";

        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api v1");
        options.OAuthClientId(clientId);
        options.OAuthUsePkce();
        options.OAuthScopes(effectiveScopes);
    });
}

app.UseHttpsRedirection();
var securityHeadersOptions = app.Services.GetRequiredService<IOptions<SecurityHeadersOptions>>().Value;
if (app.Environment.IsProduction() && securityHeadersOptions.EnableHsts)
{
    app.UseHsts();
}

app.UseRouting();

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
        var csp = $"default-src 'none'; frame-ancestors {frameAncestors}; base-uri 'none';";

        if (securityHeadersOptions.CspMode == CspMode.Enforce)
        {
            context.Response.Headers["Content-Security-Policy"] = csp;
        }
        else if (securityHeadersOptions.CspMode == CspMode.ReportOnly)
        {
            context.Response.Headers["Content-Security-Policy-Report-Only"] = csp;
        }

        if (context.Request.Path.StartsWithSegments("/app") || context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
            context.Response.Headers.Pragma = "no-cache";
        }

        return Task.CompletedTask;
    });

    await next();
});
app.UseCors("Web");
if (hardeningOptions.Enabled && hardeningOptions.RateLimiting.Enabled)
{
    app.UseRateLimiter();
}
app.UseAuthentication();
app.UseMiddleware<EndpointAuthorizationMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapHub<AppHub>("/hubs/app").RequireAuthorization();

var api = app.MapGroup("/api");

api.MapGet("/public", () => new { message = "Hello from public endpoint." })
    .AllowAnonymous();

api.MapGet("/me", (HttpContext context, IMapper mapper) =>
    {
        var model = new UserProfileModel
        {
            Sub = context.User.FindFirst("sub")?.Value ?? string.Empty,
            Name = context.User.Identity?.Name,
            Email = context.User.FindFirst("email")?.Value,
            Permissions = context.User.FindAll(AuthConstants.ClaimTypes.Permission)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };

        var dto = mapper.Map<MeDto>(model);
        return Results.Ok(dto);
    })
    .RequireAuthorization();

api.MapGet("/admin/ping", () => Results.Ok(new { status = "pong" }))
    .RequireAuthorization("RequireSystemAdmin");

api.MapGet("/integrations/authserver/ping", async (AuthServerClient client, CancellationToken cancellationToken) =>
    Results.Ok(await client.PingAsync(cancellationToken)))
    .RequireAuthorization("RequireSystemAdmin");

api.MapGet("/integrations/cms/ping", async (CmsClient client, CancellationToken cancellationToken) =>
    Results.Ok(await client.PingAsync(cancellationToken)))
    .RequireAuthorization("RequireSystemAdmin");

var content = app.MapGroup("/app");

var internalTokensOptions = app.Services.GetRequiredService<IOptions<InternalTokenOptions>>().Value;
if (internalTokensOptions.Jwks.Enabled)
{
    app.MapGet(internalTokensOptions.Jwks.Path, (HttpContext context, IOptions<InternalTokenOptions> optionsAccessor, InternalJwksService jwksService) =>
        {
            var options = optionsAccessor.Value;
            if (options.Jwks.RequireInternalApiKey)
            {
                if (string.IsNullOrWhiteSpace(options.Jwks.InternalApiKey))
                {
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }

                if (!context.Request.Headers.TryGetValue(options.Jwks.InternalApiKeyHeaderName, out var headerValue)
                    || !SecureEquals(headerValue.ToString(), options.Jwks.InternalApiKey))
                {
                    return Results.Unauthorized();
                }
            }

            return Results.Json(jwksService.GetJwksDocument());
        })
        .AllowAnonymous();
}

content.MapGet("/items", async (AppServerClient client, HttpContext context, CancellationToken cancellationToken) =>
    {
        var response = await client.GetItemsAsync(cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        context.Response.StatusCode = (int)response.StatusCode;
        return Results.Text(payload, "application/json");
    })
    .RequireAuthorization("RequireContentRead");

content.MapPost("/items", async (AppServerClient client, HttpContext context, CancellationToken cancellationToken) =>
    {
        var body = await context.Request.ReadFromJsonAsync<object>(cancellationToken: cancellationToken) ?? new { };
        var response = await client.CreateItemAsync(body, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        context.Response.StatusCode = (int)response.StatusCode;
        return Results.Text(payload, "application/json");
    })
    .RequireAuthorization("RequireContentWrite");

api.MapGet("/integrations/authserver/contract", (PolicyMapClient policyMapClient, ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("AuthContractDiagnostics");
        var policyMapDiagnostics = policyMapClient.GetDiagnostics();
        logger.LogInformation(
            "AuthServer contract diagnostics requested for environment {EnvironmentName} with policy map status {RefreshStatus}.",
            app.Environment.EnvironmentName,
            policyMapDiagnostics.RefreshStatus);

        return Results.Ok(new
        {
            issuer = effectiveIssuer,
            audience = effectiveAudience,
            requiredScope = effectiveRequiredScope,
            permissionClaimType = AuthConstants.ClaimTypes.Permission,
            schemeName = authScheme,
            policyMapEndpointUrl = policyMapDiagnostics.EndpointUrl,
            policyMapLastRefreshUtc = policyMapDiagnostics.LastRefreshUtc,
            policyMapLastFailureUtc = policyMapDiagnostics.LastFailureUtc,
            policyMapLastFailureReason = policyMapDiagnostics.LastFailureReason,
            policyMapRefreshStatus = policyMapDiagnostics.RefreshStatus,
            policyMapEntryCount = policyMapDiagnostics.EntryCount,
            swaggerEnabled = enableSwagger,
            environmentName = app.Environment.EnvironmentName,
            validationMode = configuredValidationMode.ToString(),
            clockSkewSeconds = effectiveClockSkew
        });
    })
    .RequireAuthorization("RequireSystemAdmin");


api.MapGet("/admin/auth/contract", (PolicyMapClient policyMapClient, ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("AuthContractDiagnostics");
        var policyMapDiagnostics = policyMapClient.GetDiagnostics();
        logger.LogInformation(
            "AuthServer contract diagnostics requested for environment {EnvironmentName} with policy map status {RefreshStatus}.",
            app.Environment.EnvironmentName,
            policyMapDiagnostics.RefreshStatus);

        return Results.Ok(new
        {
            issuer = effectiveIssuer,
            audience = effectiveAudience,
            requiredScope = effectiveRequiredScope,
            permissionClaimType = AuthConstants.ClaimTypes.Permission,
            schemeName = authScheme,
            policyMapEndpointUrl = policyMapDiagnostics.EndpointUrl,
            policyMapLastRefreshUtc = policyMapDiagnostics.LastRefreshUtc,
            policyMapLastFailureUtc = policyMapDiagnostics.LastFailureUtc,
            policyMapLastFailureReason = policyMapDiagnostics.LastFailureReason,
            policyMapRefreshStatus = policyMapDiagnostics.RefreshStatus,
            policyMapEntryCount = policyMapDiagnostics.EntryCount,
            swaggerEnabled = enableSwagger,
            environmentName = app.Environment.EnvironmentName,
            validationMode = configuredValidationMode.ToString(),
            clockSkewSeconds = effectiveClockSkew
        });
    })
    .RequireAuthorization("RequireSystemAdmin");


static bool SecureEquals(string left, string right)
{
    if (left.Length != right.Length)
    {
        return false;
    }

    return CryptographicOperations.FixedTimeEquals(
        System.Text.Encoding.UTF8.GetBytes(left),
        System.Text.Encoding.UTF8.GetBytes(right));
}

app.Run();
