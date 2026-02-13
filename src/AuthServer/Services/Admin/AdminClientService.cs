using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.Admin.Validation;
using AuthServer.Services.Governance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Admin;

public sealed class AdminClientService : IAdminClientService
{
    private static readonly IReadOnlyDictionary<string, string> GrantTypeReverseMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [OpenIddictConstants.GrantTypes.AuthorizationCode] = "authorization_code",
        [OpenIddictConstants.GrantTypes.RefreshToken] = "refresh_token",
        [OpenIddictConstants.GrantTypes.ClientCredentials] = "client_credentials",
        [OpenIddictConstants.GrantTypes.Password] = "password"
    };

    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenIncidentService _incidentService;
    private readonly IClientPresetRegistry _presetRegistry;
    private readonly ClientConfigComposer _configComposer;
    private const string BrandingPropertyName = "branding";

    private readonly ClientConfigValidator _configValidator;
    private readonly OidcClientLintService _lintService;
    private readonly IWebHostEnvironment _hostEnvironment;

    public AdminClientService(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        TokenIncidentService incidentService,
        IClientPresetRegistry presetRegistry,
        ClientConfigComposer configComposer,
        ClientConfigValidator configValidator,
        OidcClientLintService lintService,
        IWebHostEnvironment hostEnvironment)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _incidentService = incidentService;
        _presetRegistry = presetRegistry;
        _configComposer = configComposer;
        _configValidator = configValidator;
        _lintService = lintService;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<PagedResult<AdminClientListItem>> GetClientsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var items = new List<AdminClientListItem>();
        var clientStates = await _dbContext.ClientStates
            .AsNoTracking()
            .ToDictionaryAsync(state => state.ApplicationId, cancellationToken);

        await foreach (var application in _applicationManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var clientId = await _applicationManager.GetClientIdAsync(application, cancellationToken) ?? string.Empty;
            var displayName = await _applicationManager.GetDisplayNameAsync(application, cancellationToken);

            if (!string.IsNullOrWhiteSpace(search)
                && !clientId.Contains(search, StringComparison.OrdinalIgnoreCase)
                && !(displayName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                continue;
            }

            var id = await _applicationManager.GetIdAsync(application, cancellationToken) ?? string.Empty;
            var clientType = await _applicationManager.GetClientTypeAsync(application, cancellationToken) ?? string.Empty;
            var enabled = ResolveEnabled(id, clientStates);
            var branding = await GetBrandingAsync(application, cancellationToken);

            items.Add(new AdminClientListItem(
                id,
                clientId,
                displayName,
                clientType,
                enabled,
                HasBranding(branding)));
        }

        var totalCount = items.Count;
        var pagedItems = items
            .OrderBy(item => item.ClientId, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<AdminClientListItem>(pagedItems, totalCount, page, pageSize);
    }

    public async Task<AdminClientDetail?> GetClientAsync(string id, CancellationToken cancellationToken)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        return application is null ? null : await BuildDetailAsync(application, cancellationToken);
    }

    public async Task<AdminClientSaveResponse> CreateClientAsync(AdminClientCreateRequest request, CancellationToken cancellationToken)
    {
        var normalized = await NormalizeAsync(request, cancellationToken);
        var config = normalized.Configuration;
        var existing = await _applicationManager.FindByClientIdAsync(config.ClientId, cancellationToken);
        if (existing is not null)
        {
            var errors = new AdminValidationErrors();
            errors.Add("clientId", "Client ID already exists.");
            throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = config.ClientId,
            DisplayName = config.DisplayName,
            ClientType = config.ClientType
        };

        foreach (var uri in config.RedirectUris)
        {
            descriptor.RedirectUris.Add(uri);
        }

        foreach (var uri in config.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(uri);
        }

        foreach (var permission in BuildPermissions(config))
        {
            descriptor.Permissions.Add(permission);
        }

        foreach (var requirement in BuildRequirements(config))
        {
            descriptor.Requirements.Add(requirement);
        }

        string? clientSecret = null;
        if (string.Equals(config.ClientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.Ordinal))
        {
            clientSecret = GenerateSecret();
            descriptor.ClientSecret = clientSecret;
        }

        SetBranding(descriptor, config.Branding);

        await _applicationManager.CreateAsync(descriptor, cancellationToken);

        var application = await _applicationManager.FindByClientIdAsync(config.ClientId, cancellationToken)
            ?? throw new InvalidOperationException("Created client was not found.");

        var applicationId = await _applicationManager.GetIdAsync(application, cancellationToken) ?? string.Empty;
        await UpsertClientStateAsync(applicationId, config.Enabled, config.Profile, config.PresetId, config.PresetVersion, config.OverridesJson, cancellationToken);

        var detail = await BuildDetailAsync(application, cancellationToken);
        await LogAuditAsync("client.created", "OpenIddictApplication", detail.Id, new
        {
            detail.ClientId,
            detail.DisplayName,
            detail.ClientType,
            detail.GrantTypes,
            detail.Scopes,
            detail.Enabled,
            detail.Profile,
            detail.PresetId,
            detail.PresetVersion
        });

        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            await _incidentService.LogIncidentAsync(
                "client_secret_created",
                "low",
                null,
                detail.ClientId,
                new { detail.ClientId },
                GetActorUserId(),
                cancellationToken: cancellationToken);
        }

        return new AdminClientSaveResponse(detail, normalized.Findings, null);
    }

    public async Task<AdminClientSaveResponse?> UpdateClientAsync(string id, AdminClientUpdateRequest request, CancellationToken cancellationToken)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var currentState = await _dbContext.ClientStates.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicationId == id, cancellationToken);
        if (currentState?.SystemManaged == true && !request.ForceSystemManagedEdit)
        {
            var stateErrors = new AdminValidationErrors();
            stateErrors.Add("presetId", "This client is system managed. Set forceSystemManagedEdit to true to modify it.");
            throw new AdminValidationException("Invalid client configuration.", stateErrors.ToDictionary());
        }

        var normalized = await NormalizeAsync(request, cancellationToken);
        var config = normalized.Configuration;
        var existing = await _applicationManager.FindByClientIdAsync(config.ClientId, cancellationToken);
        if (existing is not null)
        {
            var existingId = await _applicationManager.GetIdAsync(existing, cancellationToken);
            if (!string.Equals(existingId, id, StringComparison.Ordinal))
            {
                var errors = new AdminValidationErrors();
                errors.Add("clientId", "Client ID already exists.");
                throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
            }
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);

        descriptor.ClientId = config.ClientId;
        descriptor.DisplayName = config.DisplayName;
        descriptor.ClientType = config.ClientType;

        descriptor.RedirectUris.Clear();
        foreach (var uri in config.RedirectUris)
        {
            descriptor.RedirectUris.Add(uri);
        }

        descriptor.PostLogoutRedirectUris.Clear();
        foreach (var uri in config.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(uri);
        }

        descriptor.Permissions.Clear();
        foreach (var permission in BuildPermissions(config))
        {
            descriptor.Permissions.Add(permission);
        }

        descriptor.Requirements.Clear();
        foreach (var requirement in BuildRequirements(config))
        {
            descriptor.Requirements.Add(requirement);
        }

        if (string.Equals(config.ClientType, OpenIddictConstants.ClientTypes.Public, StringComparison.Ordinal))
        {
            descriptor.ClientSecret = null;
        }

        SetBranding(descriptor, config.Branding);

        await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);

        var applicationId = await _applicationManager.GetIdAsync(application, cancellationToken) ?? string.Empty;
        await UpsertClientStateAsync(applicationId, config.Enabled, config.Profile, config.PresetId, config.PresetVersion, config.OverridesJson, cancellationToken);

        var detail = await BuildDetailAsync(application, cancellationToken);
        await LogAuditAsync("client.updated", "OpenIddictApplication", detail.Id, new
        {
            detail.ClientId,
            detail.DisplayName,
            detail.ClientType,
            detail.GrantTypes,
            detail.Scopes,
            detail.Enabled,
            detail.Profile,
            detail.PresetId,
            detail.PresetVersion
        });

        return new AdminClientSaveResponse(detail, normalized.Findings);
    }

    public async Task<AdminClientPreviewResponse> PreviewAsync(AdminClientCreateRequest request, CancellationToken cancellationToken)
    {
        var normalized = await NormalizeAsync(request, cancellationToken);
        return new AdminClientPreviewResponse(normalized.Effective, normalized.Findings);
    }

    public async Task<AdminClientSecretResult?> RotateSecretAsync(string id, CancellationToken cancellationToken)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var clientType = await _applicationManager.GetClientTypeAsync(application, cancellationToken);
        if (!string.Equals(clientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.Ordinal))
        {
            var errors = new AdminValidationErrors();
            errors.Add("clientType", "Client secret rotation is only supported for confidential clients.");
            throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);

        var newSecret = GenerateSecret();
        descriptor.ClientSecret = newSecret;

        await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);

        var detail = await BuildDetailAsync(application, cancellationToken);
        await LogAuditAsync("client.secret.rotated", "OpenIddictApplication", detail.Id, new
        {
            detail.ClientId
        });

        await _incidentService.LogIncidentAsync(
            "client_secret_rotated",
            "medium",
            null,
            detail.ClientId,
            new { detail.ClientId },
            GetActorUserId(),
            cancellationToken: cancellationToken);

        return new AdminClientSecretResult(detail, null);
    }

    public async Task<AdminClientDetail?> SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var applicationId = await _applicationManager.GetIdAsync(application, cancellationToken) ?? string.Empty;
        await UpsertClientStateAsync(applicationId, enabled, profile: null, presetId: null, presetVersion: null, overridesJson: null, cancellationToken);

        var detail = await BuildDetailAsync(application, cancellationToken);
        await LogAuditAsync(enabled ? "client.enabled" : "client.disabled", "OpenIddictApplication", detail.Id, new
        {
            detail.ClientId,
            detail.Enabled
        });

        return detail;
    }

    public async Task<bool> DeleteClientAsync(string id, CancellationToken cancellationToken)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return false;
        }

        var detail = await BuildDetailAsync(application, cancellationToken);
        await _applicationManager.DeleteAsync(application, cancellationToken);

        var existingState = await _dbContext.ClientStates
            .FirstOrDefaultAsync(state => state.ApplicationId == id, cancellationToken);
        if (existingState is not null)
        {
            _dbContext.ClientStates.Remove(existingState);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await LogAuditAsync("client.deleted", "OpenIddictApplication", detail.Id, new
        {
            detail.ClientId,
            detail.DisplayName
        });

        return true;
    }

    public async Task<IReadOnlyList<AdminClientScopeItem>> GetScopesAsync(CancellationToken cancellationToken)
    {
        var scopes = new List<AdminClientScopeItem>();

        await foreach (var scope in _scopeManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            var name = await _scopeManager.GetNameAsync(scope, cancellationToken);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var displayName = await _scopeManager.GetDisplayNameAsync(scope, cancellationToken);
            scopes.Add(new AdminClientScopeItem(name, displayName));
        }

        return scopes
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<AdminClientDetail> BuildDetailAsync(object application, CancellationToken cancellationToken)
    {
        var id = await _applicationManager.GetIdAsync(application, cancellationToken) ?? string.Empty;
        var clientId = await _applicationManager.GetClientIdAsync(application, cancellationToken) ?? string.Empty;
        var displayName = await _applicationManager.GetDisplayNameAsync(application, cancellationToken);
        var clientType = await _applicationManager.GetClientTypeAsync(application, cancellationToken) ?? string.Empty;
        var enabled = await GetEnabledAsync(id, cancellationToken);
        var permissions = await _applicationManager.GetPermissionsAsync(application, cancellationToken);
        var requirements = await _applicationManager.GetRequirementsAsync(application, cancellationToken);
        var redirectUris = await _applicationManager.GetRedirectUrisAsync(application, cancellationToken);
        var postLogoutUris = await _applicationManager.GetPostLogoutRedirectUrisAsync(application, cancellationToken);
        var state = await _dbContext.ClientStates.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicationId == id, cancellationToken);

        var grantTypes = ParseGrantTypes(permissions);
        var scopes = ParseScopes(permissions);
        var pkceRequired = requirements.Contains(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange, StringComparer.Ordinal);
        var policy = ClientPolicySnapshot.From(state?.OverridesJson);
        var branding = await GetBrandingAsync(application, cancellationToken);

        return new AdminClientDetail(
            id,
            clientId,
            displayName,
            clientType,
            enabled,
            grantTypes,
            pkceRequired,
            policy.ClientApplicationType,
            permissions.Contains(OpenIddictConstants.Permissions.Endpoints.Introspection, StringComparer.Ordinal) || policy.AllowIntrospection,
            policy.AllowUserCredentials,
            policy.AllowedScopesForPasswordGrant,
            scopes,
            redirectUris.Select(uri => uri.ToString()).ToArray(),
            postLogoutUris.Select(uri => uri.ToString()).ToArray(),
            state?.Profile,
            state?.AppliedPresetId,
            state?.AppliedPresetVersion,
            state?.SystemManaged ?? false,
            branding);
    }

    private sealed record NormalizedClientRequest(AdminClientConfiguration Configuration, AdminClientEffectiveConfig Effective, IReadOnlyList<FindingDto> Findings);

    private async Task<NormalizedClientRequest> NormalizeAsync(AdminClientCreateRequest request, CancellationToken cancellationToken)
    {
        return await NormalizeAsync(
            request.ClientId,
            request.DisplayName,
            request.ClientType,
            request.Enabled,
            request.GrantTypes,
            request.PkceRequired,
            request.ClientApplicationType,
            request.AllowIntrospection,
            request.AllowUserCredentials,
            request.AllowedScopesForPasswordGrant,
            request.Scopes,
            request.RedirectUris,
            request.PostLogoutRedirectUris,
            request.Profile,
            request.PresetId,
            request.PresetVersion,
            request.Overrides,
            request.Branding,
            cancellationToken);
    }

    private async Task<NormalizedClientRequest> NormalizeAsync(AdminClientUpdateRequest request, CancellationToken cancellationToken)
    {
        return await NormalizeAsync(
            request.ClientId,
            request.DisplayName,
            request.ClientType,
            request.Enabled,
            request.GrantTypes,
            request.PkceRequired,
            request.ClientApplicationType,
            request.AllowIntrospection,
            request.AllowUserCredentials,
            request.AllowedScopesForPasswordGrant,
            request.Scopes,
            request.RedirectUris,
            request.PostLogoutRedirectUris,
            request.Profile,
            request.PresetId,
            request.PresetVersion,
            request.Overrides,
            request.Branding,
            cancellationToken);
    }

    private async Task<NormalizedClientRequest> NormalizeAsync(
        string? clientId,
        string? displayName,
        string? clientType,
        bool enabled,
        IReadOnlyList<string> grantTypes,
        bool pkceRequired,
        string? clientApplicationType,
        bool allowIntrospection,
        bool allowUserCredentials,
        IReadOnlyList<string> allowedScopesForPasswordGrant,
        IReadOnlyList<string> scopes,
        IReadOnlyList<string> redirectUris,
        IReadOnlyList<string> postLogoutRedirectUris,
        string? profile,
        string? presetId,
        int? presetVersion,
        JsonElement? overrides,
        ClientBranding? branding,
        CancellationToken cancellationToken)
    {
        var errors = new AdminValidationErrors();
        var normalizedClientId = OidcValidationSpec.NormalizeClientId(clientId, errors, "clientId");

        if (string.IsNullOrWhiteSpace(presetId))
        {
            errors.Add("presetId", "Preset is required.");
            throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
        }

        var preset = _presetRegistry.GetById(presetId);
        if (preset is null)
        {
            errors.Add("presetId", "Unknown preset.");
            throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
        }

        if (presetVersion is not null && presetVersion != preset.Version)
        {
            errors.Add("presetVersion", "Preset version is out of date.");
        }

        if (!string.IsNullOrWhiteSpace(profile) && !string.Equals(profile, preset.Profile, StringComparison.Ordinal))
        {
            errors.Add("profile", "Profile must match selected preset.");
        }

        var mergedOverrides = BuildOverrides(
            grantTypes,
            pkceRequired,
            scopes,
            redirectUris,
            postLogoutRedirectUris,
            clientType,
            clientApplicationType,
            allowIntrospection,
            allowUserCredentials,
            allowedScopesForPasswordGrant,
            overrides);
        var effective = _configComposer.Compose(preset, mergedOverrides);
        var normalizedClientApplicationType = OidcValidationSpec.NormalizeClientApplicationType(clientApplicationType ?? effective.ClientApplicationType, errors, "clientApplicationType");
        _configValidator.Validate(effective, errors);

        var normalizedScopes = effective.Scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var scope in normalizedScopes)
        {
            var scopeEntity = await _scopeManager.FindByNameAsync(scope, cancellationToken);
            if (scopeEntity is null)
            {
                errors.Add("scopes", $"Unknown scope: {scope}.");
            }
        }

        if (errors.HasErrors)
        {
            throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
        }

        var normalizedRedirectUris = OidcValidationSpec.NormalizeRedirectUris(effective.RedirectUris, errors, "redirectUris", "Redirect URI");
        var normalizedPostLogoutUris = OidcValidationSpec.NormalizeRedirectUris(effective.PostLogoutRedirectUris, errors, "postLogoutRedirectUris", "Post logout redirect URI");
        var normalizedBranding = NormalizeBranding(branding, errors);

        if (errors.HasErrors)
        {
            throw new AdminValidationException("Invalid client configuration.", errors.ToDictionary());
        }

        var configuration = new AdminClientConfiguration(
            normalizedClientId!,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            effective.ClientType,
            normalizedClientApplicationType,
            enabled,
            OidcValidationSpec.NormalizeGrantTypes(effective.GrantTypes, errors, "grantTypes").ToList(),
            effective.PkceRequired,
            effective.AllowIntrospection,
            effective.AllowUserCredentials,
            effective.AllowedScopesForPasswordGrant
                .Where(scope => !string.IsNullOrWhiteSpace(scope))
                .Select(scope => scope.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            normalizedScopes,
            normalizedRedirectUris,
            normalizedPostLogoutUris,
            preset.Profile,
            preset.Id,
            preset.Version,
            mergedOverrides?.GetRawText(),
            normalizedBranding);

        var findings = _lintService.Analyze(effective);
        return new NormalizedClientRequest(configuration, effective, findings);
    }

    private static JsonElement? BuildOverrides(
        IReadOnlyList<string> grantTypes,
        bool pkceRequired,
        IReadOnlyList<string> scopes,
        IReadOnlyList<string> redirectUris,
        IReadOnlyList<string> postLogoutRedirectUris,
        string? clientType,
        string? clientApplicationType,
        bool allowIntrospection,
        bool allowUserCredentials,
        IReadOnlyList<string> allowedScopesForPasswordGrant,
        JsonElement? incoming)
    {
        if (incoming is not null && incoming.Value.ValueKind == JsonValueKind.Object)
        {
            return incoming;
        }

        var payload = new
        {
            grantTypes,
            pkceRequired,
            scopes,
            redirectUris,
            postLogoutRedirectUris,
            clientType,
            clientApplicationType,
            allowIntrospection,
            allowUserCredentials,
            allowedScopesForPasswordGrant
        };

        return JsonSerializer.SerializeToElement(payload);
    }

    private ClientBranding? NormalizeBranding(ClientBranding? branding, AdminValidationErrors errors)
    {
        if (branding is null)
        {
            return null;
        }

        var displayName = NormalizeOptionalText(branding.DisplayName);
        var logoUrl = NormalizeBrandingUrl(branding.LogoUrl, errors, "branding.logoUrl", "Logo URL");
        var homeUrl = NormalizeBrandingUrl(branding.HomeUrl, errors, "branding.homeUrl", "Home URL");
        var privacyUrl = NormalizeBrandingUrl(branding.PrivacyUrl, errors, "branding.privacyUrl", "Privacy URL");
        var termsUrl = NormalizeBrandingUrl(branding.TermsUrl, errors, "branding.termsUrl", "Terms URL");

        var normalized = new ClientBranding(displayName, logoUrl, homeUrl, privacyUrl, termsUrl);
        return HasBranding(normalized) ? normalized : null;
    }

    private string? NormalizeBrandingUrl(string? rawUrl, AdminValidationErrors errors, string field, string label)
    {
        var normalized = NormalizeOptionalText(rawUrl);
        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            errors.Add(field, $"{label} must be an absolute URL.");
            return null;
        }

        if (string.Equals(uri.Scheme, "javascript", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, "data", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, "file", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(field, $"{label} uses a prohibited URL scheme.");
            return null;
        }

        var allowHttp = _hostEnvironment.IsDevelopment();
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !(allowHttp && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)))
        {
            var requirement = allowHttp ? "HTTPS (HTTP allowed in development only)" : "HTTPS";
            errors.Add(field, $"{label} must use {requirement}.");
            return null;
        }

        return uri.ToString();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool HasBranding(ClientBranding? branding)
    {
        return branding is not null
            && (!string.IsNullOrWhiteSpace(branding.DisplayName)
                || !string.IsNullOrWhiteSpace(branding.LogoUrl)
                || !string.IsNullOrWhiteSpace(branding.HomeUrl)
                || !string.IsNullOrWhiteSpace(branding.PrivacyUrl)
                || !string.IsNullOrWhiteSpace(branding.TermsUrl));
    }

    private static void SetBranding(OpenIddictApplicationDescriptor descriptor, ClientBranding? branding)
    {
        descriptor.Properties.Remove(BrandingPropertyName);
        if (HasBranding(branding))
        {
            descriptor.Properties[BrandingPropertyName] = JsonSerializer.SerializeToElement(branding);
        }
    }

    private async Task<ClientBranding?> GetBrandingAsync(object application, CancellationToken cancellationToken)
    {
        var properties = await _applicationManager.GetPropertiesAsync(application, cancellationToken);
        if (!properties.TryGetValue(BrandingPropertyName, out var element)
            || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var branding = element.Deserialize<ClientBranding>();
        return HasBranding(branding) ? branding : null;
    }

    private static IEnumerable<string> BuildPermissions(AdminClientConfiguration config)
    {
        var permissions = new HashSet<string>
        {
            OpenIddictConstants.Permissions.Endpoints.Token
        };

        if (config.RedirectUris.Count > 0 || config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode))
        {
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
        }

        if (config.PostLogoutRedirectUris.Count > 0)
        {
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
        }

        if (config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode)
            || config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.RefreshToken))
        {
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);
        }

        if (config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode))
        {
            permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
        }

        if (config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.RefreshToken))
        {
            permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
        }

        if (config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.ClientCredentials))
        {
            permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
        }

        if (config.GrantTypes.Contains(OpenIddictConstants.GrantTypes.Password))
        {
            permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
        }

        if (config.AllowIntrospection)
        {
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
        }

        foreach (var scope in config.Scopes)
        {
            permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        return permissions;
    }

    private static IEnumerable<string> BuildRequirements(AdminClientConfiguration config)
    {
        if (config.PkceRequired)
        {
            return [OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange];
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> ParseGrantTypes(IReadOnlyList<string> permissions)
    {
        var grantTypes = new List<string>();

        if (permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode, StringComparer.Ordinal))
        {
            grantTypes.Add(GrantTypeReverseMap[OpenIddictConstants.GrantTypes.AuthorizationCode]);
        }

        if (permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.RefreshToken, StringComparer.Ordinal))
        {
            grantTypes.Add(GrantTypeReverseMap[OpenIddictConstants.GrantTypes.RefreshToken]);
        }

        if (permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials, StringComparer.Ordinal))
        {
            grantTypes.Add(GrantTypeReverseMap[OpenIddictConstants.GrantTypes.ClientCredentials]);
        }

        if (permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.Password, StringComparer.Ordinal))
        {
            grantTypes.Add(GrantTypeReverseMap[OpenIddictConstants.GrantTypes.Password]);
        }

        return grantTypes;
    }

    private static IReadOnlyList<string> ParseScopes(IReadOnlyList<string> permissions)
    {
        return permissions
            .Where(permission => permission.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope, StringComparison.Ordinal))
            .Select(permission => permission.Substring(OpenIddictConstants.Permissions.Prefixes.Scope.Length))
            .ToList();
    }

    private static bool ResolveEnabled(string applicationId, IReadOnlyDictionary<string, ClientState> states)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return true;
        }

        return states.TryGetValue(applicationId, out var state) ? state.Enabled : true;
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private async Task<bool> GetEnabledAsync(string applicationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return true;
        }

        var state = await _dbContext.ClientStates
            .AsNoTracking()
            .FirstOrDefaultAsync(clientState => clientState.ApplicationId == applicationId, cancellationToken);

        return state?.Enabled ?? true;
    }

    private async Task UpsertClientStateAsync(
        string applicationId,
        bool enabled,
        string? profile,
        string? presetId,
        int? presetVersion,
        string? overridesJson,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return;
        }

        var state = await _dbContext.ClientStates
            .FirstOrDefaultAsync(clientState => clientState.ApplicationId == applicationId, cancellationToken);

        if (state is null)
        {
            _dbContext.ClientStates.Add(new ClientState
            {
                ApplicationId = applicationId,
                Enabled = enabled,
                Profile = profile,
                AppliedPresetId = presetId,
                AppliedPresetVersion = presetVersion,
                OverridesJson = overridesJson,
                UpdatedUtc = DateTime.UtcNow
            });
        }
        else
        {
            state.Enabled = enabled;
            if (!string.IsNullOrWhiteSpace(profile))
            {
                state.Profile = profile;
            }

            if (!string.IsNullOrWhiteSpace(presetId))
            {
                state.AppliedPresetId = presetId;
                state.AppliedPresetVersion = presetVersion;
                state.OverridesJson = overridesJson;
            }

            state.UpdatedUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task LogAuditAsync(string action, string targetType, string targetId, object data)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = GetActorUserId(),
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = JsonSerializer.Serialize(data)
        });

        await _dbContext.SaveChangesAsync();
    }

    private Guid? GetActorUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private sealed record AdminClientConfiguration(
        string ClientId,
        string? DisplayName,
        string ClientType,
        string ClientApplicationType,
        bool Enabled,
        IReadOnlyList<string> GrantTypes,
        bool PkceRequired,
        bool AllowIntrospection,
        bool AllowUserCredentials,
        IReadOnlyList<string> AllowedScopesForPasswordGrant,
        IReadOnlyList<string> Scopes,
        IReadOnlyList<Uri> RedirectUris,
        IReadOnlyList<Uri> PostLogoutRedirectUris,
        string Profile,
        string PresetId,
        int PresetVersion,
        string? OverridesJson,
        ClientBranding? Branding);
}
