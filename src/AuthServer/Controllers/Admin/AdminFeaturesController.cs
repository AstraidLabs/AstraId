using AuthServer.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AuthServer.Controllers.Admin;
/// <summary>
/// Exposes HTTP endpoints for admin features.
/// </summary>

[ApiController]
[Route("admin/api/features")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminFeaturesController : ControllerBase
{
    private readonly IOptions<AuthServerAuthFeaturesOptions> _featuresOptions;

    public AdminFeaturesController(IOptions<AuthServerAuthFeaturesOptions> featuresOptions)
    {
        _featuresOptions = featuresOptions;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            enablePasswordGrant = _featuresOptions.Value.EnablePasswordGrant
        });
    }
}
