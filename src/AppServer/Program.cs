using System.Text;
using AppServer.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var authServerIssuer = builder.Configuration["AuthServer:Issuer"] ?? "https://localhost:7001/";

builder.Services.AddOptions<InternalTokenOptions>()
    .Bind(builder.Configuration.GetSection(InternalTokenOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "Internal token signing key is required.")
    .ValidateOnStart();

var internalOptions = builder.Configuration.GetSection(InternalTokenOptions.SectionName).Get<InternalTokenOptions>() ?? new InternalTokenOptions();
if (string.IsNullOrWhiteSpace(internalOptions.SigningKey))
{
    throw new InvalidOperationException("InternalTokens:SigningKey must be configured.");
}

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

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

var content = app.MapGroup("/app");

content.MapGet("/items", (ICurrentUser currentUser) =>
    Results.Ok(new { owner = currentUser.Subject, permissions = currentUser.Permissions, items = new[] { "sample-item" } }))
    .RequireAuthorization("ContentRead");

content.MapPost("/items", (ICurrentUser currentUser) =>
    Results.Ok(new { createdBy = currentUser.Subject, tenant = currentUser.Tenant ?? "default", result = "created" }))
    .RequireAuthorization("ContentWrite");

app.Run();
