using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Options;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.Governance;
using AuthServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/security")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminSecurityPoliciesController : ControllerBase
{
    private readonly KeyRotationPolicyService _keyRotationPolicyService;
    private readonly AdminTokenPolicyService _tokenPolicyService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IHostEnvironment _environment;
    private readonly IOptionsMonitor<GovernanceGuardrailsOptions> _guardrails;
    private readonly TokenIncidentService _incidentService;

    public AdminSecurityPoliciesController(
        KeyRotationPolicyService keyRotationPolicyService,
        AdminTokenPolicyService tokenPolicyService,
        ApplicationDbContext dbContext,
        IHostEnvironment environment,
        IOptionsMonitor<GovernanceGuardrailsOptions> guardrails,
        TokenIncidentService incidentService)
    {
        _keyRotationPolicyService = keyRotationPolicyService;
        _tokenPolicyService = tokenPolicyService;
        _dbContext = dbContext;
        _environment = environment;
        _guardrails = guardrails;
        _incidentService = incidentService;
    }

    [HttpGet("key-rotation-policy")]
    public async Task<ActionResult<AdminKeyRotationPolicyResponse>> GetKeyRotationPolicy(CancellationToken cancellationToken)
    {
        var policy = await _keyRotationPolicyService.GetPolicyAsync(cancellationToken);
        var guardrails = _guardrails.CurrentValue;

        return Ok(new AdminKeyRotationPolicyResponse(
            new AdminKeyRotationPolicyValues(
                policy.Enabled,
                policy.RotationIntervalDays,
                policy.GracePeriodDays,
                policy.JwksCacheMarginMinutes,
                policy.NextRotationUtc,
                policy.LastRotationUtc),
            new AdminKeyRotationPolicyGuardrails(
                guardrails.MinRotationIntervalDays,
                guardrails.MaxRotationIntervalDays,
                guardrails.MinGracePeriodDays,
                guardrails.MaxGracePeriodDays,
                guardrails.MinJwksCacheMarginMinutes,
                guardrails.MaxJwksCacheMarginMinutes,
                guardrails.PreventDisableRotationInProduction)));
    }

    [HttpPut("key-rotation-policy")]
    public async Task<ActionResult<AdminKeyRotationPolicyResponse>> UpdateKeyRotationPolicy(
        [FromBody] AdminKeyRotationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var guardrails = _guardrails.CurrentValue;
        var validation = AdminValidation.ValidateKeyRotationPolicy(request, guardrails);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid key rotation policy.").ApplyDefaults(HttpContext));
        }

        if (_environment.IsProduction()
            && guardrails.PreventDisableRotationInProduction
            && !request.Enabled)
        {
            if (!request.BreakGlass || string.IsNullOrWhiteSpace(request.Reason))
            {
                return ValidationProblem(new ValidationProblemDetails
                {
                    Title = "Rotation disable requires break-glass confirmation.",
                    Detail = "Provide breakGlass=true and a reason to disable rotation in production."
                }.ApplyDefaults(HttpContext));
            }

            await _incidentService.LogIncidentAsync(
                "key_rotation_disabled",
                "critical",
                null,
                null,
                new { request.Reason },
                GetActorUserId(),
                cancellationToken: cancellationToken);
        }

        var updated = await _keyRotationPolicyService.UpdatePolicyAsync(
            request.Enabled,
            request.RotationIntervalDays,
            request.GracePeriodDays,
            request.JwksCacheMarginMinutes,
            GetActorUserId(),
            cancellationToken);

        await LogAuditAsync("key-rotation-policy.updated", "KeyRotationPolicy", updated.Id.ToString(), new
        {
            updated.Enabled,
            updated.RotationIntervalDays,
            updated.GracePeriodDays,
            updated.NextRotationUtc,
            updated.LastRotationUtc
        }, cancellationToken);

        await _incidentService.LogIncidentAsync(
            "key_rotation_policy_changed",
            "medium",
            null,
            null,
            new
            {
                updated.Enabled,
                updated.RotationIntervalDays,
                updated.GracePeriodDays,
                updated.JwksCacheMarginMinutes
            },
            GetActorUserId(),
            cancellationToken: cancellationToken);

        return Ok(new AdminKeyRotationPolicyResponse(
            new AdminKeyRotationPolicyValues(
                updated.Enabled,
                updated.RotationIntervalDays,
                updated.GracePeriodDays,
                updated.JwksCacheMarginMinutes,
                updated.NextRotationUtc,
                updated.LastRotationUtc),
            new AdminKeyRotationPolicyGuardrails(
                guardrails.MinRotationIntervalDays,
                guardrails.MaxRotationIntervalDays,
                guardrails.MinGracePeriodDays,
                guardrails.MaxGracePeriodDays,
                guardrails.MinJwksCacheMarginMinutes,
                guardrails.MaxJwksCacheMarginMinutes,
                guardrails.PreventDisableRotationInProduction)));
    }

    [HttpGet("token-policy")]
    public async Task<ActionResult<AdminTokenPolicyConfig>> GetTokenPolicy(CancellationToken cancellationToken)
    {
        var config = await _tokenPolicyService.GetConfigAsync(cancellationToken);
        return Ok(config);
    }

    [HttpPut("token-policy")]
    public async Task<ActionResult<AdminTokenPolicyConfig>> UpdateTokenPolicy(
        [FromBody] AdminTokenPolicyValues request,
        CancellationToken cancellationToken)
    {
        var validation = AdminValidation.ValidateTokenPolicy(request, _guardrails.CurrentValue);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid token policy.").ApplyDefaults(HttpContext));
        }

        var updated = await _tokenPolicyService.UpdateConfigAsync(request, cancellationToken);
        return Ok(updated);
    }

    private Guid? GetActorUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task LogAuditAsync(string action, string targetType, string targetId, object data, CancellationToken cancellationToken)
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

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
