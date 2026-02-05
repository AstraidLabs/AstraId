using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.Governance;
using AuthServer.Services.Tokens;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace AuthServer.Services.Admin;

public sealed class AdminTokenPolicyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenPolicyService _tokenPolicyService;
    private readonly TokenIncidentService _incidentService;
    private readonly IOptionsMonitorCache<OpenIddictServerOptions> _optionsCache;

    public AdminTokenPolicyService(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        TokenPolicyService tokenPolicyService,
        TokenIncidentService incidentService,
        IOptionsMonitorCache<OpenIddictServerOptions> optionsCache)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _tokenPolicyService = tokenPolicyService;
        _incidentService = incidentService;
        _optionsCache = optionsCache;
    }

    public async Task<AdminTokenPolicyConfig> GetConfigAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _tokenPolicyService.GetEffectivePolicyAsync(cancellationToken);
        var guardrails = _tokenPolicyService.GetGuardrails();
        return new AdminTokenPolicyConfig(ToValues(snapshot), ToGuardrails(guardrails));
    }

    public async Task<AdminTokenPolicyConfig> UpdateConfigAsync(
        AdminTokenPolicyValues request,
        CancellationToken cancellationToken)
    {
        var before = await _tokenPolicyService.GetEffectivePolicyAsync(cancellationToken);
        var updated = await _tokenPolicyService.UpdatePolicyAsync(ToSnapshot(request), GetActorUserId(), cancellationToken);

        _optionsCache.TryRemove(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        await LogAuditAsync("tokens.policy.updated", "TokenPolicy", "global", new
        {
            previous = before,
            current = updated
        });

        await _incidentService.LogIncidentAsync(
            "token_policy_changed",
            "medium",
            null,
            null,
            new { previous = before, current = updated },
            GetActorUserId(),
            cancellationToken: cancellationToken);

        var guardrails = _tokenPolicyService.GetGuardrails();
        return new AdminTokenPolicyConfig(ToValues(updated), ToGuardrails(guardrails));
    }

    private static TokenPolicySnapshot ToSnapshot(AdminTokenPolicyValues config)
    {
        return new TokenPolicySnapshot(
            config.AccessTokenMinutes,
            config.IdentityTokenMinutes,
            config.AuthorizationCodeMinutes,
            config.RefreshTokenDays,
            config.RefreshRotationEnabled,
            config.RefreshReuseDetectionEnabled,
            config.RefreshReuseLeewaySeconds,
            config.ClockSkewSeconds);
    }

    private static AdminTokenPolicyValues ToValues(TokenPolicySnapshot snapshot)
    {
        return new AdminTokenPolicyValues(
            snapshot.AccessTokenMinutes,
            snapshot.IdentityTokenMinutes,
            snapshot.AuthorizationCodeMinutes,
            snapshot.RefreshTokenDays,
            snapshot.RefreshRotationEnabled,
            snapshot.RefreshReuseDetectionEnabled,
            snapshot.RefreshReuseLeewaySeconds,
            snapshot.ClockSkewSeconds);
    }

    private static AdminTokenPolicyGuardrails ToGuardrails(GovernanceGuardrailsOptions guardrails)
    {
        return new AdminTokenPolicyGuardrails(
            guardrails.MinAccessTokenMinutes,
            guardrails.MaxAccessTokenMinutes,
            guardrails.MinIdentityTokenMinutes,
            guardrails.MaxIdentityTokenMinutes,
            guardrails.MinAuthorizationCodeMinutes,
            guardrails.MaxAuthorizationCodeMinutes,
            guardrails.MinRefreshTokenDays,
            guardrails.MaxRefreshTokenDays,
            guardrails.MinClockSkewSeconds,
            guardrails.MaxClockSkewSeconds);
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
}
