using System.Security.Claims;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;
/// <summary>
/// Exposes HTTP endpoints for admin revocation.
/// </summary>

[ApiController]
[Route("admin/api/security/revoke")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminRevocationController : ControllerBase
{
    private readonly TokenRevocationService _revocationService;
    private readonly TokenIncidentService _incidentService;
    private readonly ApplicationDbContext _dbContext;

    public AdminRevocationController(
        TokenRevocationService revocationService,
        TokenIncidentService incidentService,
        ApplicationDbContext dbContext)
    {
        _revocationService = revocationService;
        _incidentService = incidentService;
        _dbContext = dbContext;
    }

    [HttpPost("user/{userId:guid}")]
    public async Task<ActionResult<AdminRevocationResult>> RevokeUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _revocationService.RevokeUserAsync(userId, cancellationToken);
        await LogAuditAsync("revocation.user", "Token", userId.ToString(), result, cancellationToken);
        await _incidentService.LogIncidentAsync(
            "revocation_user",
            "medium",
            userId,
            null,
            new { userId, result },
            GetActorUserId(),
            cancellationToken: cancellationToken);

        return Ok(new AdminRevocationResult(result.TokensRevoked, result.AuthorizationsRevoked));
    }

    [HttpPost("client/{clientId}")]
    public async Task<ActionResult<AdminRevocationResult>> RevokeClient(string clientId, CancellationToken cancellationToken)
    {
        var result = await _revocationService.RevokeClientAsync(clientId, cancellationToken);
        await LogAuditAsync("revocation.client", "Token", clientId, result, cancellationToken);
        await _incidentService.LogIncidentAsync(
            "revocation_client",
            "medium",
            null,
            clientId,
            new { clientId, result },
            GetActorUserId(),
            cancellationToken: cancellationToken);

        return Ok(new AdminRevocationResult(result.TokensRevoked, result.AuthorizationsRevoked));
    }

    [HttpPost("user/{userId:guid}/client/{clientId}")]
    public async Task<ActionResult<AdminRevocationResult>> RevokeUserClient(
        Guid userId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var result = await _revocationService.RevokeUserClientAsync(userId, clientId, cancellationToken);
        await LogAuditAsync("revocation.user-client", "Token", $"{userId}:{clientId}", result, cancellationToken);
        await _incidentService.LogIncidentAsync(
            "revocation_user_client",
            "medium",
            userId,
            clientId,
            new { userId, clientId, result },
            GetActorUserId(),
            cancellationToken: cancellationToken);

        return Ok(new AdminRevocationResult(result.TokensRevoked, result.AuthorizationsRevoked));
    }

    private Guid? GetActorUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task LogAuditAsync(string action, string targetType, string targetId, TokenRevocationResult result, CancellationToken cancellationToken)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = GetActorUserId(),
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = JsonSerializer.Serialize(result)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
