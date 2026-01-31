using Company.Auth.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;

namespace Company.Auth.Api;

public static class CompanyAuthExtensions
{
    public static IServiceCollection AddCompanyAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        string apiResourceNameOrAudience)
    {
        var issuer = configuration["Auth:Issuer"] ?? AuthConstants.DefaultIssuer;

        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

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

        services.AddAuthorization(options =>
        {
            PermissionPolicies.AddPermissionPolicy(options, PermissionPolicies.DefaultPermissionPolicyName, "system.admin");
        });

        return services;
    }
}

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
}
