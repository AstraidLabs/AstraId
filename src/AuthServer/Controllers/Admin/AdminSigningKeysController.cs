using AuthServer.Data;
using AuthServer.Options;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using AuthServer.Services;
using AuthServer.Services.SigningKeys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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

    public AdminSigningKeysController(
        SigningKeyRingService keyRingService,
        AdminSigningKeyService adminSigningKeyService,
        IOptionsMonitor<AuthServerSigningKeyOptions> options,
        ISigningKeyRotationState rotationState)
    {
        _keyRingService = keyRingService;
        _adminSigningKeyService = adminSigningKeyService;
        _options = options;
        _rotationState = rotationState;
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
        var intervalDays = Math.Max(1, _options.CurrentValue.RotationIntervalDays);
        var nextRotation = active is null
            ? (DateTime?)null
            : (active.ActivatedUtc ?? active.CreatedUtc).AddDays(intervalDays);

        var response = new AdminSigningKeyRingResponse(
            items,
            nextRotation,
            _rotationState.NextCheckUtc,
            _rotationState.LastRotationUtc,
            Math.Max(0, _options.CurrentValue.PreviousKeyRetentionDays),
            _options.CurrentValue.Enabled,
            Math.Max(1, _options.CurrentValue.RotationIntervalDays),
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
}
