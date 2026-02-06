using AuthServer.Options;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using AuthServer.Services;
using AuthServer.Services.Governance;
using AuthServer.Services.SigningKeys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AuthServer.Data;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/signing-keys")]
[Route("admin/api/security/keys/signing")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminSigningKeysController : ControllerBase
{
    private readonly SigningKeyRingService _keyRingService;
    private readonly AdminSigningKeyService _adminSigningKeyService;
    private readonly IOptionsMonitor<AuthServerSigningKeyOptions> _options;
    private readonly ISigningKeyRotationState _rotationState;
    private readonly KeyRotationPolicyService _policyService;
    private readonly SigningKeyJwksService _jwksService;

    public AdminSigningKeysController(
        SigningKeyRingService keyRingService,
        AdminSigningKeyService adminSigningKeyService,
        IOptionsMonitor<AuthServerSigningKeyOptions> options,
        ISigningKeyRotationState rotationState,
        KeyRotationPolicyService policyService,
        SigningKeyJwksService jwksService)
    {
        _keyRingService = keyRingService;
        _adminSigningKeyService = adminSigningKeyService;
        _options = options;
        _rotationState = rotationState;
        _policyService = policyService;
        _jwksService = jwksService;
    }

    [HttpGet]
    public async Task<ActionResult<AdminSigningKeyRingResponse>> GetKeys(CancellationToken cancellationToken)
    {
        var keys = await _keyRingService.GetAllAsync(cancellationToken);
        var items = keys.Select(entry => new AdminSigningKeyListItem(
            entry.Kid,
            entry.Status.ToString(),
            entry.CreatedUtc,
            entry.ActivatedUtc,
            entry.RetireAfterUtc,
            entry.RetiredUtc,
            entry.RevokedUtc,
            entry.Algorithm,
            entry.KeyType,
            entry.NotBeforeUtc,
            entry.NotAfterUtc,
            entry.Status is SigningKeyStatus.Active or SigningKeyStatus.Previous)).ToList();

        var active = keys.FirstOrDefault(entry => entry.Status == SigningKeyStatus.Active);
        var policy = await _policyService.GetPolicyAsync(cancellationToken);
        var intervalDays = Math.Max(1, policy.RotationIntervalDays);
        var nextRotation = policy.NextRotationUtc
            ?? (active is null ? null : (active.ActivatedUtc ?? active.CreatedUtc).AddDays(intervalDays));

        var response = new AdminSigningKeyRingResponse(
            items,
            nextRotation,
            _rotationState.NextCheckUtc,
            _rotationState.LastRotationUtc,
            Math.Max(0, policy.GracePeriodDays),
            policy.Enabled,
            Math.Max(1, policy.RotationIntervalDays),
            Math.Max(1, _options.CurrentValue.CheckPeriodMinutes));

        return Ok(response);
    }

    [HttpPost("rotate")]
    public async Task<ActionResult<AdminSigningKeyRotationResponse>> Rotate(CancellationToken cancellationToken)
    {
        var result = await _adminSigningKeyService.RotateNowAsync(cancellationToken);
        return Ok(new AdminSigningKeyRotationResponse(
            result.NewActive.Kid,
            result.PreviousActive?.Kid,
            result.NewActive.ActivatedUtc ?? result.NewActive.CreatedUtc));
    }

    [HttpPost("{kid}/retire")]
    public async Task<ActionResult<AdminSigningKeyListItem>> Retire(string kid, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _adminSigningKeyService.RetireAsync(kid, cancellationToken);
            if (entry is null)
            {
                return NotFound();
            }

            return Ok(new AdminSigningKeyListItem(
                entry.Kid,
                entry.Status.ToString(),
                entry.CreatedUtc,
                entry.ActivatedUtc,
                entry.RetireAfterUtc,
                entry.RetiredUtc,
                entry.RevokedUtc,
                entry.Algorithm,
                entry.KeyType,
                entry.NotBeforeUtc,
                entry.NotAfterUtc,
                entry.Status is SigningKeyStatus.Active or SigningKeyStatus.Previous));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to retire key.",
                Detail = exception.Message
            }.ApplyDefaults(HttpContext));
        }
    }

    [HttpPost("{kid}/revoke")]
    public async Task<ActionResult> Revoke(string kid, CancellationToken cancellationToken)
    {
        var result = await _adminSigningKeyService.RevokeAsync(kid, cancellationToken);
        return result == SigningKeyRevokeResult.NotFound ? NotFound() : NoContent();
    }

    [HttpGet("jwks")]
    public async Task<ActionResult<AdminSigningKeyJwksResponse>> GetJwksPreview(CancellationToken cancellationToken)
    {
        var jwksJson = await _jwksService.BuildPublicJwksJsonAsync(cancellationToken);
        return Ok(new AdminSigningKeyJwksResponse(jwksJson));
    }
}
