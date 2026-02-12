using Api.Contracts;
using Api.HealthChecks;
using Api.Integrations;
using Api.Middleware;
using Api.Models;
using Api.Options;
using Api.Services;
using Company.Auth.Api;
using Company.Auth.Contracts;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var configuredIssuer = builder.Configuration["Auth:Issuer"];
var configuredAudience = builder.Configuration["Auth:Audience"];

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
}

var effectiveIssuer = string.IsNullOrWhiteSpace(configuredIssuer)
    ? AuthConstants.DefaultIssuer
    : configuredIssuer;
var effectiveAudience = string.IsNullOrWhiteSpace(configuredAudience)
    ? "api"
    : configuredAudience;
var effectiveScopes = builder.Configuration.GetSection("Auth:Scopes").Get<string[]>() ?? ["api"];

var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.NewConfig<UserProfileModel, MeDto>();

builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddCompanyAuth(builder.Configuration, effectiveAudience);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSystemAdmin", policy =>
        policy.RequirePermission("system.admin"));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
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
builder.Services.Configure<HttpOptions>(builder.Configuration.GetSection("Http"));

builder.Services.AddTransient<CorrelationIdHandler>();

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
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

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

var enableSwagger = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:EnableInProduction");
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
if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseRouting();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none';";
        return Task.CompletedTask;
    });

    await next();
});
app.UseCors("Web");
app.UseAuthentication();
app.UseMiddleware<EndpointAuthorizationMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();

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

api.MapGet("/integrations/auth/contract", (PolicyMapClient policyMapClient) =>
    {
        var policyMapDiagnostics = policyMapClient.GetDiagnostics();
        return Results.Ok(new
        {
            issuer = effectiveIssuer,
            audience = effectiveAudience,
            permissionClaimType = AuthConstants.ClaimTypes.Permission,
            scopes = effectiveScopes,
            policyMapEndpointUrl = policyMapDiagnostics.EndpointUrl,
            policyMapLastRefreshUtc = policyMapDiagnostics.LastRefreshUtc,
            policyMapEntryCount = policyMapDiagnostics.EntryCount,
            policyMapLastFailureUtc = policyMapDiagnostics.LastFailureUtc,
            validationMode = "discovery+jwks"
        });
    })
    .RequireAuthorization("RequireSystemAdmin");

app.Run();
