using AuthServer.Options;
using AuthServer.Services;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.SigningKeys;
using AuthServer.Services.Tokens;
using AuthServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AuthServer.Options;
using AuthServer.Services.Governance;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/tokens")]
[Route("admin/api/security/tokens")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminTokensController : ControllerBase
{
    private readonly AdminTokenPolicyService _policyService;
    private readonly SigningKeyRingService _keyRingService;
    private readonly ISigningKeyRotationState _rotationState;
    private readonly IOptionsMonitor<GovernanceGuardrailsOptions> _guardrails;
    private readonly KeyRotationPolicyService _keyRotationPolicyService;

    public AdminTokensController(
        AdminTokenPolicyService policyService,
        SigningKeyRingService keyRingService,
        ISigningKeyRotationState rotationState,
        IOptionsMonitor<GovernanceGuardrailsOptions> guardrails,
        KeyRotationPolicyService keyRotationPolicyService)
    {
        _policyService = policyService;
        _keyRingService = keyRingService;
        _rotationState = rotationState;
        _guardrails = guardrails;
        _keyRotationPolicyService = keyRotationPolicyService;
    }

    [HttpGet("config")]
    [HttpGet("policy")]
    public async Task<ActionResult<AdminTokenPolicyConfig>> GetConfig(CancellationToken cancellationToken)
    {
        var config = await _policyService.GetConfigAsync(cancellationToken);
        return Ok(config);
    }

    [HttpPut("config")]
    [HttpPut("policy")]
    public async Task<ActionResult<AdminTokenPolicyConfig>> UpdateConfig(
        [FromBody] AdminTokenPolicyValues request,
        CancellationToken cancellationToken)
    {
        var validation = AdminValidation.ValidateTokenPolicy(request, _guardrails.CurrentValue);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid token policy.").ApplyDefaults(HttpContext));
        }

        var updated = await _policyService.UpdateConfigAsync(request, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("status")]
    public async Task<ActionResult<AdminTokenPolicyStatus>> GetStatus(CancellationToken cancellationToken)
    {
        var config = await _policyService.GetConfigAsync(cancellationToken);
        var active = await _keyRingService.GetActiveAsync(cancellationToken);
        var rotationPolicy = await _keyRotationPolicyService.GetPolicyAsync(cancellationToken);
        var response = new AdminTokenPolicyStatus(
            active?.Kid,
            rotationPolicy.Enabled,
            _rotationState.NextCheckUtc,
            config);

        return Ok(response);
    }
}
