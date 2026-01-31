using Api.Models;
using Api.Contracts;
using Mapster;
using MapsterMapper;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.NewConfig<UserProfileModel, MeDto>();

builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer("https://localhost:7001/");
        options.AddAudiences("api");
        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization();

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
app.UseAuthorization();

app.MapGet("/api/public", () => new { message = "Hello from public endpoint." });

app.MapGet("/api/me", (HttpContext context, IMapper mapper) =>
    {
        var model = new UserProfileModel
        {
            Sub = context.User.FindFirst("sub")?.Value ?? string.Empty,
            Name = context.User.Identity?.Name,
            Email = context.User.FindFirst("email")?.Value
        };

        var dto = mapper.Map<MeDto>(model);
        return Results.Ok(dto);
    })
    .RequireAuthorization();

app.Run();
