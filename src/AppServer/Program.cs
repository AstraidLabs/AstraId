using System.Text;
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
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var issuer = context.Principal?.FindFirst("iss")?.Value;
                if (string.Equals(issuer, authServerIssuer, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(issuer, internalOptions.Issuer, StringComparison.Ordinal))
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

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new LocalDashboardAuthorizationFilter()]
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

app.Run();
