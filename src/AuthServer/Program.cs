using AuthServer.Authorization;
using AuthServer.Data;
using AuthServer.Options;
using AuthServer.Seeding;
using AuthServer.Services;
using AuthServer.Services.Cors;
using AuthServer.Services.Cryptography;
using AuthServer.Services.Admin;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
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

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminRoleService, AdminRoleService>();
builder.Services.AddScoped<IAdminPermissionAdminService, AdminPermissionAdminService>();
builder.Services.AddScoped<IAdminApiResourceService, AdminApiResourceService>();
builder.Services.AddScoped<IAdminEndpointService, AdminEndpointService>();
builder.Services.AddScoped<IAdminClientService, AdminClientService>();
builder.Services.AddScoped<IAdminOidcScopeService, AdminOidcScopeService>();
builder.Services.AddScoped<IAdminOidcResourceService, AdminOidcResourceService>();
builder.Services.AddScoped<IClientStateService, ClientStateService>();
builder.Services.AddSingleton<AuthRateLimiter>();
builder.Services.AddSingleton<MfaChallengeStore>();
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
            return !hasPermissionClaim || context.User.HasClaim(AuthConstants.ClaimTypes.Permission, "system.admin");
        });
    });
});

builder.Services.Configure<AuthServerUiOptions>(builder.Configuration.GetSection(AuthServerUiOptions.SectionName));
builder.Services.AddSingleton<UiUrlBuilder>();
builder.Services.AddSingleton<ReturnUrlValidator>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<BootstrapAdminOptions>(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services.Configure<AuthServerCertificateOptions>(builder.Configuration.GetSection(AuthServerCertificateOptions.SectionName));

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

builder.Services.AddCors();

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
               .SetCryptographyEndpointUris(".well-known/jwks")
               .SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               .SetUserInfoEndpointUris("connect/userinfo")
               .SetEndSessionEndpointUris("connect/logout")
               .SetRevocationEndpointUris("connect/revocation");

        options.AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow();

        options.RegisterScopes(AuthServerScopeRegistry.AllowedScopes.ToArray());

        options.DisableAccessTokenEncryption();

        var certificateOptions = builder.Configuration
            .GetSection(AuthServerCertificateOptions.SectionName)
            .Get<AuthServerCertificateOptions>() ?? new AuthServerCertificateOptions();

        ConfigureCertificates(options, certificateOptions, builder.Environment);

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();
    });

builder.Services.AddHostedService<AuthBootstrapHostedService>();

var app = builder.Build();

var emailOptions = app.Services.GetRequiredService<IOptions<EmailOptions>>().Value;
ValidateEmailOptions(emailOptions, app.Environment);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

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
app.UseAuthorization();

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

static void ConfigureCertificates(
    OpenIddictServerBuilder options,
    AuthServerCertificateOptions certificateOptions,
    IWebHostEnvironment environment)
{
    var signingCertificate = CertificateLoader.TryLoadCertificate(certificateOptions.Signing);
    var encryptionCertificate = CertificateLoader.TryLoadCertificate(certificateOptions.Encryption);

    if (signingCertificate is null)
    {
        if (environment.IsDevelopment())
        {
            options.AddDevelopmentSigningCertificate();
        }
        else
        {
            throw new InvalidOperationException(
                "Signing certificate is required. Configure AuthServer:Certificates:Signing.");
        }
    }
    else
    {
        options.AddSigningCertificate(signingCertificate);
    }

    if (encryptionCertificate is null)
    {
        if (signingCertificate is not null)
        {
            options.AddEncryptionCertificate(signingCertificate);
        }
        else if (environment.IsDevelopment())
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
