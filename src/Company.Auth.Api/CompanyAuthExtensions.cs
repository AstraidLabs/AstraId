using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using System.Text.Json;
using Company.Auth.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Validation;
using OpenIddict.Validation.AspNetCore;

namespace Company.Auth.Api;

/// <summary>
/// Provides company auth extensions functionality.
/// </summary>
public static class CompanyAuthExtensions
{
    public const string JwtScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    public const string IntrospectionScheme = "AuthIntrospection";
    public const string HybridScheme = "AuthHybrid";
    public const string IntrospectionHttpClientName = "AuthIntrospection";

    public static IServiceCollection AddCompanyAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        string apiResourceNameOrAudience)
    {
        var issuer = configuration["Auth:Issuer"] ?? AuthConstants.DefaultIssuer;
        var mode = AuthValidationModeParser.Parse(configuration["Auth:ValidationMode"]);

        services.Configure<AuthValidationOptions>(options =>
        {
            options.ValidationMode = mode;
            options.Issuer = issuer;
            options.Audience = apiResourceNameOrAudience;
            options.ClockSkewSeconds = configuration.GetValue<int?>("Auth:ClockSkewSeconds");
            options.Introspection.ClientId = configuration["Auth:Introspection:ClientId"];
            options.Introspection.ClientSecret = configuration["Auth:Introspection:ClientSecret"];
            options.Introspection.Scope = configuration["Auth:Introspection:Scope"];
        });

        services.AddMemoryCache();
        services.AddHttpClient(IntrospectionHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = mode switch
                {
                    AuthValidationMode.Jwt => JwtScheme,
                    AuthValidationMode.Introspection => IntrospectionScheme,
                    _ => HybridScheme
                };
                options.DefaultChallengeScheme = options.DefaultAuthenticateScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, AuthIntrospectionHandler>(IntrospectionScheme, _ => { })
            .AddPolicyScheme(HybridScheme, HybridScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var token = TokenReader.ReadBearerToken(context.Request.Headers.Authorization);
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        return JwtScheme;
                    }

                    return TokenReader.LooksLikeJwt(token)
                        ? JwtScheme
                        : IntrospectionScheme;
                };
            });

        services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(new Uri(issuer));

                if (!string.IsNullOrWhiteSpace(apiResourceNameOrAudience))
                {
                    options.AddAudiences(apiResourceNameOrAudience);
                }

                options.UseSystemNetHttp();
                options.UseAspNetCore();
            });

        var clockSkewSeconds = configuration.GetValue<int?>("Auth:ClockSkewSeconds");
        if (clockSkewSeconds is > 0)
        {
            services.Configure<OpenIddictValidationOptions>(options =>
            {
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(clockSkewSeconds.Value);
            });
        }

        services.AddAuthorization(options =>
        {
            PermissionPolicies.AddPermissionPolicy(options, PermissionPolicies.DefaultPermissionPolicyName, "system.admin");
        });

        return services;
    }
}

/// <summary>
/// Provides token reader functionality.
/// </summary>
internal static class TokenReader
{
    public static string? ReadBearerToken(StringValues authorizationHeader)
    {
        if (StringValues.IsNullOrEmpty(authorizationHeader))
        {
            return null;
        }

        if (!AuthenticationHeaderValue.TryParse(authorizationHeader.ToString(), out var headerValue))
        {
            return null;
        }

        if (!"Bearer".Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return headerValue.Parameter;
    }

    public static bool LooksLikeJwt(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        return IsLikelyJson(parts[0]) && IsLikelyJson(parts[1]);
    }

    private static bool IsLikelyJson(string base64UrlPart)
    {
        try
        {
            var padded = base64UrlPart.Replace('-', '+').Replace('_', '/');
            var mod = padded.Length % 4;
            if (mod > 0)
            {
                padded = padded.PadRight(padded.Length + (4 - mod), '=');
            }

            var bytes = Convert.FromBase64String(padded);
            using var document = JsonDocument.Parse(bytes);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Provides auth introspection handler functionality.
/// </summary>
public sealed class AuthIntrospectionHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(45);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<AuthValidationOptions> _authOptions;

    public AuthIntrospectionHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptionsMonitor<AuthValidationOptions> authOptions)
        : base(options, logger, encoder)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _authOptions = authOptions;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = TokenReader.ReadBearerToken(Request.Headers.Authorization);
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        if (_cache.TryGetValue<AuthIntrospectionResult>(GetCacheKey(token), out var cached))
        {
            return cached!.ToAuthenticateResult(Scheme.Name);
        }

        var settings = _authOptions.CurrentValue;
        if (string.IsNullOrWhiteSpace(settings.Issuer)
            || string.IsNullOrWhiteSpace(settings.Introspection.ClientId)
            || string.IsNullOrWhiteSpace(settings.Introspection.ClientSecret))
        {
            Logger.LogWarning("Introspection authentication failed due to incomplete configuration.");
            return AuthenticateResult.Fail("Introspection configuration is incomplete.");
        }

        var introspectionUri = new Uri(new Uri(settings.Issuer.TrimEnd('/') + "/"), "connect/introspect");

        AuthIntrospectionResult result;
        try
        {
            result = await IntrospectAsync(introspectionUri, token, settings, Context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            return AuthenticateResult.Fail("Token introspection timed out.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Token introspection failed due to unexpected error.");
            return AuthenticateResult.Fail("Token introspection failed.");
        }

        _cache.Set(GetCacheKey(token), result, CacheDuration);
        return result.ToAuthenticateResult(Scheme.Name);
    }

    private async Task<AuthIntrospectionResult> IntrospectAsync(
        Uri endpoint,
        string token,
        AuthValidationOptions settings,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(CompanyAuthExtensions.IntrospectionHttpClientName);

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new FormUrlEncodedContent(CreateIntrospectionPayload(token, settings))
            };

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if ((int)response.StatusCode >= 500 && attempt == 1)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);
                    continue;
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Logger.LogWarning("Introspection endpoint rejected client authentication with status code {StatusCode}.", response.StatusCode);
                    return AuthIntrospectionResult.Fail("Introspection client authentication failed.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return AuthIntrospectionResult.Fail("Token introspection rejected the token.");
                }

                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
                return ParseIntrospectionResponse(document.RootElement);
            }
            catch (HttpRequestException) when (attempt == 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);
            }
        }

        return AuthIntrospectionResult.Fail("Token introspection failed.");
    }

    private static Dictionary<string, string> CreateIntrospectionPayload(string token, AuthValidationOptions settings)
    {
        var payload = new Dictionary<string, string>
        {
            ["token"] = token,
            ["client_id"] = settings.Introspection.ClientId!,
            ["client_secret"] = settings.Introspection.ClientSecret!
        };

        if (!string.IsNullOrWhiteSpace(settings.Introspection.Scope))
        {
            payload["scope"] = settings.Introspection.Scope;
        }

        return payload;
    }

    private static AuthIntrospectionResult ParseIntrospectionResponse(JsonElement root)
    {
        if (!root.TryGetProperty("active", out var activeElement) || activeElement.ValueKind != JsonValueKind.True)
        {
            return AuthIntrospectionResult.Fail("Token is inactive.");
        }

        var claims = new List<Claim>();
        var subject = GetString(root, "sub");
        if (!string.IsNullOrWhiteSpace(subject))
        {
            claims.Add(new Claim(OpenIddictConstants.Claims.Subject, subject));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, subject));
        }

        AddClaimIfPresent(claims, root, OpenIddictConstants.Claims.Name);
        AddClaimIfPresent(claims, root, OpenIddictConstants.Claims.Email);
        AddMultiValueClaims(claims, root, OpenIddictConstants.Claims.Audience, OpenIddictConstants.Claims.Audience);
        AddMultiValueClaims(claims, root, OpenIddictConstants.Claims.Scope, OpenIddictConstants.Claims.Scope);
        AddMultiValueClaims(claims, root, AuthConstants.ClaimTypes.Permission, AuthConstants.ClaimTypes.Permission);
        AddMultiValueClaims(claims, root, "permissions", AuthConstants.ClaimTypes.Permission);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CompanyAuthExtensions.IntrospectionScheme));
        return AuthIntrospectionResult.Success(principal);
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static void AddClaimIfPresent(List<Claim> claims, JsonElement root, string claimType)
    {
        var value = GetString(root, claimType);
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(claimType, value));
        }
    }

    private static void AddMultiValueClaims(List<Claim> claims, JsonElement root, string propertyName, string claimType)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var values = claimType == OpenIddictConstants.Claims.Scope
                ? value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : [value];

            foreach (var entry in values)
            {
                claims.Add(new Claim(claimType, entry));
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            {
                claims.Add(new Claim(claimType, item.GetString()!));
            }
        }
    }

    private static string GetCacheKey(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return $"auth-introspection:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))}";
    }
}

/// <summary>
/// Provides auth introspection result functionality.
/// </summary>
internal sealed record AuthIntrospectionResult(bool IsSuccess, ClaimsPrincipal? Principal, string? Error)
{
    public static AuthIntrospectionResult Success(ClaimsPrincipal principal) => new(true, principal, null);

    public static AuthIntrospectionResult Fail(string error) => new(false, null, error);

    public AuthenticateResult ToAuthenticateResult(string schemeName)
    {
        if (!IsSuccess || Principal is null)
        {
            return AuthenticateResult.Fail(Error ?? "Introspection failed.");
        }

        return AuthenticateResult.Success(new AuthenticationTicket(Principal, schemeName));
    }
}

/// <summary>
/// Provides configuration options for auth validation.
/// </summary>
public sealed class AuthValidationOptions
{
    public AuthValidationMode ValidationMode { get; set; } = AuthValidationMode.Jwt;

    public string Issuer { get; set; } = AuthConstants.DefaultIssuer;

    public string Audience { get; set; } = "api";

    public int? ClockSkewSeconds { get; set; }

    public AuthIntrospectionOptions Introspection { get; set; } = new();
}

/// <summary>
/// Provides configuration options for auth introspection.
/// </summary>
public sealed class AuthIntrospectionOptions
{
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? Scope { get; set; }
}

/// <summary>
/// Represents the available auth validation mode values.
/// </summary>
public enum AuthValidationMode
{
    Jwt,
    Introspection,
    Hybrid
}

/// <summary>
/// Provides auth validation mode parser functionality.
/// </summary>
public static class AuthValidationModeParser
{
    public static bool TryParse(string? value, out AuthValidationMode mode)
    {
        return Enum.TryParse(value, ignoreCase: true, out mode);
    }

    public static AuthValidationMode Parse(string? value)
    {
        return TryParse(value, out var mode)
            ? mode
            : AuthValidationMode.Jwt;
    }
}

/// <summary>
/// Provides permission policies functionality.
/// </summary>
public static class PermissionPolicies
{
    public const string DefaultPermissionPolicyName = "RequireSystemAdminPermission";

    public static void AddPermissionPolicy(
        AuthorizationOptions options,
        string policyName,
        string requiredPermission)
    {
        options.AddPolicy(policyName, policy =>
            policy.RequirePermission(requiredPermission));
    }

    public static AuthorizationPolicyBuilder RequirePermission(
        this AuthorizationPolicyBuilder builder,
        string permission)
    {
        return builder.RequireClaim(AuthConstants.ClaimTypes.Permission, permission);
    }

    public static AuthorizationPolicyBuilder RequireScope(
        this AuthorizationPolicyBuilder builder,
        string scope)
    {
        return builder.RequireClaim(OpenIddictConstants.Claims.Scope, scope);
    }
}
