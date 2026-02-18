using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AppServer.Application.Commands;
using AppServer.Application.Jobs;
using AppServer.Infrastructure.Caching;
using AppServer.Infrastructure.Events;
using AppServer.Infrastructure.Hangfire;
using AppServer.Modules.Game.Api;
using AppServer.Modules.Game.Infrastructure;
using AppServer.Security;
using Hangfire;
using Hangfire.InMemory;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var authServerIssuer = builder.Configuration["AuthServer:Issuer"] ?? "https://localhost:7001/";
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

builder.Services.AddOptions<InternalTokenOptions>()
    .Bind(builder.Configuration.GetSection(InternalTokenOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "Internal token signing key is required.")
    .Validate(options => string.Equals(options.Algorithm, "HS256", StringComparison.OrdinalIgnoreCase), "Only HS256 is supported for internal tokens.")
    .ValidateOnStart();

var internalOptions = builder.Configuration.GetSection(InternalTokenOptions.SectionName).Get<InternalTokenOptions>() ?? new InternalTokenOptions();
if (string.IsNullOrWhiteSpace(internalOptions.SigningKey))
{
    throw new InvalidOperationException("InternalTokens:SigningKey must be configured.");
}

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
builder.Services.AddGameModule(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("game-state", x =>
    {
        x.PermitLimit = 30;
        x.Window = TimeSpan.FromSeconds(30);
        x.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("game-commands", x =>
    {
        x.PermitLimit = 20;
        x.Window = TimeSpan.FromSeconds(60);
        x.QueueLimit = 0;
    });
});

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(internalOptions.SigningKey)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.SecurityToken is not JwtSecurityToken jwt
                    || !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal)
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
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHangfireDashboard("/admin/hangfire", new DashboardOptions
{
    Authorization = [new SecureDashboardAuthorizationFilter(app.Environment.IsDevelopment())]
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

var content = app.MapGroup("/app");

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

app.MapGameEndpoints();

app.Run();
