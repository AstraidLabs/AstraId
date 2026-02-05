using AuthServer.Data;
using AuthServer.Options;
using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.SigningKeys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/signing-keys")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminSigningKeysController : ControllerBase
{
    private readonly SigningKeyRingService _keyRingService;
    private readonly AdminSigningKeyService _adminSigningKeyService;
    private readonly IOptionsMonitor<AuthServerSigningKeyOptions> _options;

    public AdminSigningKeysController(
        SigningKeyRingService keyRingService,
        AdminSigningKeyService adminSigningKeyService,
        IOptionsMonitor<AuthServerSigningKeyOptions> options)
    {
        _keyRingService = keyRingService;
        _adminSigningKeyService = adminSigningKeyService;
        _options = options;
    }

    [HttpGet]
    public async Task<ActionResult<AdminSigningKeyRingResponse>> GetKeys(CancellationToken cancellationToken)
    {
        var keys = await _keyRingService.GetCurrentAsync(cancellationToken);
        var items = keys.Select(entry => new AdminSigningKeyListItem(
            entry.Kid,
            entry.Status.ToString(),
            entry.CreatedUtc,
            entry.ActivatedUtc,
            entry.RetiredUtc,
            entry.Algorithm,
            entry.KeyType,
            entry.NotBeforeUtc,
            entry.NotAfterUtc)).ToList();

        var active = keys.FirstOrDefault(entry => entry.Status == SigningKeyStatus.Active);
        var intervalDays = Math.Max(1, _options.CurrentValue.RotationIntervalDays);
        var nextRotation = active is null
            ? (DateTime?)null
            : (active.ActivatedUtc ?? active.CreatedUtc).AddDays(intervalDays);

        var response = new AdminSigningKeyRingResponse(
            items,
            nextRotation,
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
}
