using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using AuthServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/audit")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminAuditController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminAuditController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminAuditListItem>>> GetAuditLog(
        [FromQuery] string? search,
        [FromQuery] string? action,
        [FromQuery] string? targetType,
        [FromQuery] string? targetId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var validation = AdminValidation.ValidateAuditFilters(fromUtc, toUtc);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid audit filters."));
        }

        var query = _dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(entry =>
                entry.Action.Contains(search) ||
                entry.TargetType.Contains(search) ||
                (entry.TargetId != null && entry.TargetId.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(entry => entry.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            query = query.Where(entry => entry.TargetType.Contains(targetType));
        }

        if (!string.IsNullOrWhiteSpace(targetId))
        {
            query = query.Where(entry => entry.TargetId != null && entry.TargetId.Contains(targetId));
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(entry => entry.TimestampUtc >= fromUtc.Value.UtcDateTime);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(entry => entry.TimestampUtc <= toUtc.Value.UtcDateTime);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderByDescending(entry => entry.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var actorIds = entries
            .Select(entry => entry.ActorUserId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var actorLookup = actorIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await _userManager.Users
                .Where(user => actorIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, user => user.Email, cancellationToken);

        var items = entries
            .Select(entry => new AdminAuditListItem(
                entry.Id,
                entry.TimestampUtc,
                entry.Action,
                entry.TargetType,
                entry.TargetId,
                entry.ActorUserId,
                entry.ActorUserId.HasValue && actorLookup.TryGetValue(entry.ActorUserId.Value, out var email)
                    ? email
                    : null,
                entry.DataJson))
            .ToList();

        return Ok(new PagedResult<AdminAuditListItem>(items, totalCount, page, pageSize));
    }
}
