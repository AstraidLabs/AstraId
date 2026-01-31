using Api.Contracts;
using Api.Middleware;
using Api.Models;
using Api.Services;
using Company.Auth.Api;
using Company.Auth.Contracts;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.NewConfig<UserProfileModel, MeDto>();

builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddCompanyAuth(builder.Configuration, builder.Configuration["Auth:Audience"] ?? "api");
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrPermission", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.HasClaim(AuthConstants.ClaimTypes.Permission, "system.admin")));
});

builder.Services.AddHttpClient();
builder.Services.Configure<PolicyMapOptions>(builder.Configuration.GetSection("Api:AuthServer"));
builder.Services.AddSingleton<PolicyMapClient>();
builder.Services.AddHostedService<PolicyMapRefreshService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("Web");
app.UseAuthentication();
app.UseMiddleware<EndpointAuthorizationMiddleware>();
app.UseAuthorization();

app.MapGet("/api/public", () => new { message = "Hello from public endpoint." });

app.MapGet("/api/me", (HttpContext context, IMapper mapper) =>
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

app.MapGet("/api/admin/ping", () => Results.Ok(new { status = "pong" }))
    .RequireAuthorization("AdminOrPermission");

app.Run();
