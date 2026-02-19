using System.IdentityModel.Tokens.Jwt;
using AppServer.Application.Commands;
using AppServer.Application.Jobs;
using AppServer.Infrastructure.Caching;
using AppServer.Infrastructure.Events;
using AppServer.Infrastructure.Hangfire;
using AppServer.Security;
using Hangfire;
using Hangfire.InMemory;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var authServerIssuer = builder.Configuration["AuthServer:Issuer"] ?? "https://localhost:7001/";
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

builder.Services.AddOptions<InternalTokenOptions>()
    .Bind(builder.Configuration.GetSection(InternalTokenOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.JwksUrl), "InternalTokens:JwksUrl is required.")
    .Validate(options => options.JwksRefreshMinutes > 0, "InternalTokens:JwksRefreshMinutes must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddOptions<AppServerMtlsOptions>()
    .Bind(builder.Configuration.GetSection(AppServerMtlsOptions.SectionName))
    .ValidateOnStart();

var internalOptions = builder.Configuration.GetSection(InternalTokenOptions.SectionName).Get<InternalTokenOptions>() ?? new InternalTokenOptions();
if (string.IsNullOrWhiteSpace(internalOptions.JwksUrl))
{
    throw new InvalidOperationException("InternalTokens:JwksUrl must be configured.");
}

var mtlsOptions = builder.Configuration.GetSection(AppServerMtlsOptions.SectionName).Get<AppServerMtlsOptions>() ?? new AppServerMtlsOptions();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        if (mtlsOptions.Enabled && mtlsOptions.RequireClientCertificate)
        {
            httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        }
    });
});

if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    throw new InvalidOperationException("Redis:ConnectionString must be configured.");
}

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<IEventPublisher, RedisEventPublisher>();
builder.Services.AddScoped<ItemCacheService>();
builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PublishArticleCommand>());

builder.Services.AddHangfire(configuration => configuration.UseInMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddScoped<GenerateThumbnailJob>();
builder.Services.AddHttpClient("InternalJwks");
var signingKeyResolver = new InternalSigningKeyResolver();
builder.Services.AddSingleton(signingKeyResolver);
builder.Services.AddSingleton<InternalJwksCache>();
builder.Services.AddHostedService<InternalJwksRefreshService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = internalOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = internalOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, kid, parameters) =>
            {
                var key = signingKeyResolver.Resolve(kid);
                if (key is not null)
                {
                    return [key];
                }

                if (internalOptions.AllowLegacyHs256 && !string.IsNullOrWhiteSpace(internalOptions.LegacyHs256Secret) && !string.Equals(internalOptions.LegacyHs256Secret, "__REPLACE_ME__", StringComparison.Ordinal))
                {
                    return [new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(internalOptions.LegacyHs256Secret))];
                }

                return [];
            },
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = internalOptions.AllowedAlgorithms
                .Select(alg => alg.ToUpperInvariant() switch
                {
                    "RS256" => SecurityAlgorithms.RsaSha256,
                    "ES256" => SecurityAlgorithms.EcdsaSha256,
                    "HS256" => SecurityAlgorithms.HmacSha256,
                    _ => string.Empty
                })
                .Where(alg => !string.IsNullOrWhiteSpace(alg))
                .ToArray(),
            ClockSkew = TimeSpan.FromSeconds(5)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.SecurityToken is not JwtSecurityToken jwt
                    || string.Equals(jwt.Header.Alg, SecurityAlgorithms.None, StringComparison.Ordinal)
                    || !jwt.Payload.Iat.HasValue
                    || !jwt.Payload.Exp.HasValue
                    || !jwt.Payload.Nbf.HasValue)
                {
                    context.Fail("Invalid internal token format.");
                    return Task.CompletedTask;
                }

                var issuer = context.Principal?.FindFirst("iss")?.Value;
                var audience = context.Principal?.FindFirst("aud")?.Value;
                if (string.Equals(issuer, authServerIssuer, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(issuer, internalOptions.Issuer, StringComparison.Ordinal)
                    || !string.Equals(audience, internalOptions.Audience, StringComparison.Ordinal))
                {
                    context.Fail("Only API-issued internal tokens are accepted.");
                    return Task.CompletedTask;
                }

                var service = context.Principal?.FindFirst("svc")?.Value ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Azp)?.Value;
                if (string.IsNullOrWhiteSpace(service) || !internalOptions.AllowedServices.Contains(service, StringComparer.Ordinal))
                {
                    context.Fail("Service claim validation failed.");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ContentRead", policy =>
        policy.RequireAssertion(context => ScopeParser.GetScopes(context.User).Contains("content.read")));
    options.AddPolicy("ContentWrite", policy =>
        policy.RequireAssertion(context => ScopeParser.GetScopes(context.User).Contains("content.write")));
    options.AddPolicy("RequireMtls", policy =>
        policy.RequireAssertion(context => !mtlsOptions.Enabled || !mtlsOptions.RequireClientCertificate || (context.Resource is HttpContext httpContext && httpContext.Connection.ClientCertificate is not null)));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var cache = scope.ServiceProvider.GetRequiredService<InternalJwksCache>();
    var refreshed = await cache.RefreshAsync(CancellationToken.None);
    var startupOptions = scope.ServiceProvider.GetRequiredService<IOptions<InternalTokenOptions>>().Value;
    var fallbackEnabled = startupOptions.AllowLegacyHs256
        && !string.IsNullOrWhiteSpace(startupOptions.LegacyHs256Secret)
        && !string.Equals(startupOptions.LegacyHs256Secret, "__REPLACE_ME__", StringComparison.Ordinal);
    if (!refreshed && !fallbackEnabled)
    {
        throw new InvalidOperationException("Failed to load JWKS and legacy fallback is not enabled.");
    }
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var options = context.RequestServices.GetRequiredService<IOptions<AppServerMtlsOptions>>().Value;
    if (options.Enabled && options.RequireClientCertificate && context.Request.Path.StartsWithSegments("/app"))
    {
        var cert = context.Connection.ClientCertificate;
        if (cert is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Client certificate required.");
            return;
        }

        var thumbprintAllowed = options.AllowedClientThumbprints.Length == 0 || options.AllowedClientThumbprints.Contains(cert.Thumbprint ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        var subjectAllowed = options.AllowedClientSubjectNames.Length == 0 || options.AllowedClientSubjectNames.Contains(cert.Subject, StringComparer.OrdinalIgnoreCase);
        if (!thumbprintAllowed || !subjectAllowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Client certificate not allowed.");
            return;
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/admin/hangfire", new DashboardOptions
{
    Authorization = [new SecureDashboardAuthorizationFilter(app.Environment.IsDevelopment())]
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

var content = app.MapGroup("/app");
content.RequireAuthorization("RequireMtls");

content.MapGet("/items", (ICurrentUser currentUser) =>
    Results.Ok(new { owner = currentUser.Subject, permissions = currentUser.Permissions, items = new[] { "sample-item" } }))
    .RequireAuthorization("ContentRead");

content.MapGet("/items/{itemId}", async (string itemId, ItemCacheService cacheService, CancellationToken cancellationToken) =>
    {
        var payload = await cacheService.GetOrCreateAsync(itemId, () =>
            Task.FromResult<object>(new { id = itemId, source = "database", fetchedAt = DateTimeOffset.UtcNow }), cancellationToken);

        return Results.Ok(payload);
    })
    .RequireAuthorization("ContentRead");

content.MapPost("/items", async (ICurrentUser currentUser, IMediator mediator, CancellationToken cancellationToken) =>
    {
        var id = Guid.NewGuid().ToString("N");
        var result = await mediator.Send(new PublishArticleCommand(id, currentUser.Tenant, currentUser.Subject), cancellationToken);
        return Results.Ok(new { createdBy = currentUser.Subject, tenant = currentUser.Tenant ?? "default", result = result.Status, articleId = result.ArticleId });
    })
    .RequireAuthorization("ContentWrite");

app.Run();
