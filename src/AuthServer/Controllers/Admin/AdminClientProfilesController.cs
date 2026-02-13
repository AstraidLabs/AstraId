using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminClientProfilesController : ControllerBase
{
    private readonly IClientProfileRegistry _profileRegistry;
    private readonly IClientPresetRegistry _presetRegistry;

    public AdminClientProfilesController(IClientProfileRegistry profileRegistry, IClientPresetRegistry presetRegistry)
    {
        _profileRegistry = profileRegistry;
        _presetRegistry = presetRegistry;
    }

    [HttpGet("client-profiles/rules")]
    public ActionResult<AdminClientProfileRulesResponse> GetRules()
    {
        var response = new AdminClientProfileRulesResponse(
            1,
            _profileRegistry.GetRules()
                .Select(rule => new AdminClientProfileRuleItem(
                    rule.Profile,
                    rule.Summary,
                    rule.AllowedGrantTypes,
                    rule.RequiresPkceForAuthorizationCode,
                    rule.RequiresClientSecret,
                    rule.AllowsRedirectUris,
                    rule.AllowOfflineAccess,
                    rule.RedirectPolicy,
                    rule.RuleCodes,
                    rule.Sections,
                    rule.RequiredFields,
                    rule.ForbiddenFields,
                    rule.ValidationPatterns,
                    rule.Explanations))
                .ToList());

        return Ok(response);
    }

    [HttpGet("client-presets")]
    public ActionResult<IReadOnlyList<AdminClientPresetListItem>> GetPresets()
    {
        var presets = _presetRegistry.GetPresets()
            .Select(preset => new AdminClientPresetListItem(preset.Id, preset.Name, preset.Profile, preset.Summary, preset.Version))
            .ToList();

        return Ok(presets);
    }

    [HttpGet("client-presets/{id}")]
    public ActionResult<AdminClientPresetDetail> GetPreset(string id)
    {
        var preset = _presetRegistry.GetById(id);
        if (preset is null)
        {
            return NotFound();
        }

        return Ok(new AdminClientPresetDetail(
            preset.Id,
            preset.Name,
            preset.Profile,
            preset.Summary,
            preset.Version,
            preset.Defaults,
            preset.LockedFields,
            preset.AllowedOverrides,
            preset.FieldMetadata));
    }
}
