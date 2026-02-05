using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using AuthServer.Models;
using AuthServer.Options;
using AuthServer.Services.Admin.Models;

namespace AuthServer.Validation;

public static class AdminValidation
{
    public const int PermissionKeyMinLength = 3;
    public const int PermissionKeyMaxLength = 100;
    public const int PermissionDescriptionMaxLength = 250;
    public const int PermissionGroupMaxLength = 100;

    public const int RoleNameMinLength = 2;
    public const int RoleNameMaxLength = 64;

    public const int EndpointPathMaxLength = 256;
    public const int EndpointTagsMaxLength = 200;

    private static readonly Regex PermissionKeyRegex = new("^[a-z0-9]+(\\.[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex RoleNameRegex = new("^[A-Za-z0-9][A-Za-z0-9 _\\.-]*$", RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET",
        "POST",
        "PUT",
        "PATCH",
        "DELETE",
        "HEAD",
        "OPTIONS"
    };

    public static AdminValidationResult ValidatePermission(AdminPermissionRequest request)
    {
        var result = new AdminValidationResult();
        var key = request.Key?.Trim() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;
        var group = request.Group?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(key))
        {
            result.AddFieldError("key", "Permission key is required.");
        }
        else
        {
            if (key.Length < PermissionKeyMinLength || key.Length > PermissionKeyMaxLength)
            {
                result.AddFieldError("key", $"Permission key must be between {PermissionKeyMinLength} and {PermissionKeyMaxLength} characters.");
            }
            if (!PermissionKeyRegex.IsMatch(key))
            {
                result.AddFieldError("key", "Permission key must use lowercase letters, numbers, and dots.");
            }
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            result.AddFieldError("description", "Permission description is required.");
        }
        else
        {
            if (description.Length > PermissionDescriptionMaxLength)
            {
                result.AddFieldError("description", $"Description must be {PermissionDescriptionMaxLength} characters or fewer.");
            }
            if (ContainsControlChars(description))
            {
                result.AddFieldError("description", "Description must not contain control characters.");
            }
        }

        if (!string.IsNullOrWhiteSpace(group))
        {
            if (group.Length > PermissionGroupMaxLength)
            {
                result.AddFieldError("group", $"Group must be {PermissionGroupMaxLength} characters or fewer.");
            }
            if (ContainsControlChars(group))
            {
                result.AddFieldError("group", "Group must not contain control characters.");
            }
        }

        return result;
    }

    public static AdminValidationResult ValidateRoleName(string? roleName)
    {
        var result = new AdminValidationResult();
        var trimmed = roleName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            result.AddFieldError("name", "Role name is required.");
            return result;
        }

        if (trimmed.Length < RoleNameMinLength || trimmed.Length > RoleNameMaxLength)
        {
            result.AddFieldError("name", $"Role name must be between {RoleNameMinLength} and {RoleNameMaxLength} characters.");
        }

        if (!RoleNameRegex.IsMatch(trimmed))
        {
            result.AddFieldError("name", "Role name must start with a letter or number and can include spaces, dots, underscores, or dashes.");
        }

        return result;
    }

    public static AdminValidationResult ValidateEndpointSync(IReadOnlyCollection<ApiEndpointSyncDto> endpoints)
    {
        var result = new AdminValidationResult();
        var seen = new HashSet<(string Method, string Path)>();

        var index = 0;
        foreach (var endpoint in endpoints)
        {
            var method = endpoint.Method?.Trim() ?? string.Empty;
            var path = endpoint.Path?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(method))
            {
                result.AddFieldError($"endpoints[{index}].method", "HTTP method is required.");
            }
            else if (!AllowedMethods.Contains(method))
            {
                result.AddFieldError($"endpoints[{index}].method", "HTTP method must be one of GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS.");
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                result.AddFieldError($"endpoints[{index}].path", "Endpoint path is required.");
            }
            else
            {
                if (path.Length > EndpointPathMaxLength)
                {
                    result.AddFieldError($"endpoints[{index}].path", $"Endpoint path must be {EndpointPathMaxLength} characters or fewer.");
                }
                if (!path.StartsWith("/", StringComparison.Ordinal))
                {
                    result.AddFieldError($"endpoints[{index}].path", "Endpoint path must start with '/'.");
                }
                if (path.Any(char.IsWhiteSpace))
                {
                    result.AddFieldError($"endpoints[{index}].path", "Endpoint path must not contain whitespace.");
                }
            }

            if (!string.IsNullOrWhiteSpace(endpoint.Tags))
            {
                if (endpoint.Tags.Length > EndpointTagsMaxLength)
                {
                    result.AddFieldError($"endpoints[{index}].tags", $"Tags must be {EndpointTagsMaxLength} characters or fewer.");
                }
                if (ContainsControlChars(endpoint.Tags))
                {
                    result.AddFieldError($"endpoints[{index}].tags", "Tags must not contain control characters.");
                }
            }

            if (!string.IsNullOrWhiteSpace(method) && !string.IsNullOrWhiteSpace(path))
            {
                var key = (method.ToUpperInvariant(), NormalizePath(path));
                if (!seen.Add(key))
                {
                    result.AddFieldError($"endpoints[{index}].path", "Duplicate method/path combination in payload.");
                }
            }

            index++;
        }

        return result;
    }

    public static AdminValidationResult ValidateEmail(string? email)
    {
        var result = new AdminValidationResult();
        var trimmed = email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            result.AddFieldError("email", "Email is required.");
            return result;
        }

        var validator = new EmailAddressAttribute();
        if (!validator.IsValid(trimmed))
        {
            result.AddFieldError("email", "Email must be a valid email address.");
        }

        return result;
    }

    public static AdminValidationResult ValidateAuditFilters(DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        var result = new AdminValidationResult();
        if (fromUtc.HasValue && toUtc.HasValue && fromUtc > toUtc)
        {
            result.AddFieldError("fromUtc", "From date must be earlier than to date.");
            result.AddFieldError("toUtc", "To date must be later than from date.");
        }

        return result;
    }

    public static AdminValidationResult ValidateKeyRotationPolicy(
        AdminKeyRotationPolicyRequest request,
        GovernanceGuardrailsOptions guardrails)
    {
        var result = new AdminValidationResult();
        ValidatePositiveInt(
            "rotationIntervalDays",
            request.RotationIntervalDays,
            guardrails.MinRotationIntervalDays,
            guardrails.MaxRotationIntervalDays,
            result);
        ValidatePositiveInt(
            "gracePeriodDays",
            request.GracePeriodDays,
            guardrails.MinGracePeriodDays,
            guardrails.MaxGracePeriodDays,
            result);

        if (request.BreakGlass && string.IsNullOrWhiteSpace(request.Reason))
        {
            result.AddFieldError("reason", "Break-glass requires a reason.");
        }

        return result;
    }

    public static AdminValidationResult ValidateTokenPolicy(
        AdminTokenPolicyValues config,
        GovernanceGuardrailsOptions guardrails)
    {
        var result = new AdminValidationResult();

        ValidatePositiveInt("accessTokenMinutes", config.AccessTokenMinutes, guardrails.MinAccessTokenMinutes, guardrails.MaxAccessTokenMinutes, result);
        ValidatePositiveInt("identityTokenMinutes", config.IdentityTokenMinutes, guardrails.MinIdentityTokenMinutes, guardrails.MaxIdentityTokenMinutes, result);
        ValidatePositiveInt("authorizationCodeMinutes", config.AuthorizationCodeMinutes, guardrails.MinAuthorizationCodeMinutes, guardrails.MaxAuthorizationCodeMinutes, result);
        ValidatePositiveInt("refreshTokenDays", config.RefreshTokenDays, guardrails.MinRefreshTokenDays, guardrails.MaxRefreshTokenDays, result);
        ValidatePositiveInt("clockSkewSeconds", config.ClockSkewSeconds, guardrails.MinClockSkewSeconds, guardrails.MaxClockSkewSeconds, result);

        if (config.RefreshReuseLeewaySeconds < 0 || config.RefreshReuseLeewaySeconds > 3600)
        {
            result.AddFieldError(
                "refreshReuseLeewaySeconds",
                "Reuse leeway must be between 0 and 3600 seconds.");
        }

        return result;
    }

    private static void ValidatePositiveInt(
        string field,
        int value,
        int min,
        int max,
        AdminValidationResult result)
    {
        if (value < min || value > max)
        {
            result.AddFieldError(field, $"Value must be between {min} and {max}.");
        }
    }

    private static bool ContainsControlChars(string value)
    {
        return value.Any(char.IsControl);
    }

    private static string NormalizePath(string path)
    {
        var trimmed = path.Trim();
        return trimmed.Length > 1 && trimmed.EndsWith("/", StringComparison.Ordinal)
            ? trimmed.TrimEnd('/')
            : trimmed;
    }
}
