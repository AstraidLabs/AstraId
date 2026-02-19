using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.Governance;
using AuthServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/security")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminOAuthAdvancedPolicyController : ControllerBase
{
    private readonly IOAuthAdvancedPolicyProvider _provider;
    private readonly ApplicationDbContext _db;
    private readonly IHostEnvironment _environment;

    public AdminOAuthAdvancedPolicyController(IOAuthAdvancedPolicyProvider provider, ApplicationDbContext db, IHostEnvironment environment)
    {
        _provider = provider;
        _db = db;
        _environment = environment;
    }

    [HttpGet("oauth-advanced-policy")]
    public async Task<ActionResult<AdminOAuthAdvancedPolicyResponse>> Get(CancellationToken cancellationToken)
    {
        var snapshot = await _provider.GetCurrentAsync(cancellationToken);
        return Ok(new AdminOAuthAdvancedPolicyResponse(ToAdmin(snapshot)));
    }

    [HttpPut("oauth-advanced-policy")]
    public async Task<ActionResult<AdminOAuthAdvancedPolicyResponse>> Put([FromBody] UpdateAdminOAuthAdvancedPolicyRequest request, CancellationToken cancellationToken)
    {
        var validation = AdminValidation.ValidateOAuthAdvancedPolicy(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid OAuth advanced policy.").ApplyDefaults(HttpContext));
        }

        if (_environment.IsProduction() && !request.BreakGlass && (request.TokenExchangeEnabled || request.FrontChannelLogoutEnabled))
        {
            var details = new ValidationProblemDetails
            {
                Title = "Break-glass confirmation required in production.",
                Detail = "Set breakGlass=true to enable token exchange or front-channel logout in production.",
                Status = StatusCodes.Status422UnprocessableEntity
            }.ApplyDefaults(HttpContext);
            return new ObjectResult(details) { StatusCode = StatusCodes.Status422UnprocessableEntity };
        }

        var before = await _provider.GetCurrentAsync(cancellationToken);
        try
        {
            var updated = await _provider.UpdateAsync(
                new OAuthAdvancedPolicySnapshot(
                    request.DeviceFlowEnabled,
                    request.DeviceFlowUserCodeTtlMinutes,
                    request.DeviceFlowPollingIntervalSeconds,
                    request.TokenExchangeEnabled,
                    request.TokenExchangeAllowedClientIds,
                    request.TokenExchangeAllowedAudiences,
                    request.RefreshRotationEnabled,
                    request.RefreshReuseDetectionEnabled,
                    request.RefreshReuseAction,
                    request.BackChannelLogoutEnabled,
                    request.FrontChannelLogoutEnabled,
                    request.LogoutTokenTtlMinutes,
                    DateTime.UtcNow,
                    GetActorUserId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    request.RowVersion),
                request.RowVersion,
                GetActorUserId(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            await LogAuditAsync(before, updated, cancellationToken);
            return Ok(new AdminOAuthAdvancedPolicyResponse(ToAdmin(updated)));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Policy update conflict.",
                Detail = "The policy was changed by another admin. Reload and retry.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private AdminOAuthAdvancedPolicy ToAdmin(OAuthAdvancedPolicySnapshot snapshot)
        => new(
            snapshot.DeviceFlowEnabled,
            snapshot.DeviceFlowUserCodeTtlMinutes,
            snapshot.DeviceFlowPollingIntervalSeconds,
            snapshot.TokenExchangeEnabled,
            snapshot.TokenExchangeAllowedClientIds,
            snapshot.TokenExchangeAllowedAudiences,
            snapshot.RefreshRotationEnabled,
            snapshot.RefreshReuseDetectionEnabled,
            snapshot.RefreshReuseAction,
            snapshot.BackChannelLogoutEnabled,
            snapshot.FrontChannelLogoutEnabled,
            snapshot.LogoutTokenTtlMinutes,
            snapshot.UpdatedAtUtc,
            snapshot.UpdatedByUserId,
            snapshot.UpdatedByIp,
            snapshot.RowVersion);

    private Guid? GetActorUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task LogAuditAsync(OAuthAdvancedPolicySnapshot before, OAuthAdvancedPolicySnapshot after, CancellationToken cancellationToken)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = GetActorUserId(),
            Action = "security.oauth_advanced_policy.updated",
            TargetType = "OAuthAdvancedPolicy",
            TargetId = "global",
            DataJson = JsonSerializer.Serialize(new { before, after })
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
