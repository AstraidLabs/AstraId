using AuthServer.Data;
using AuthServer.Services;
using AuthServer.Services.Admin.Models;
using AuthServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/diagnostics/errors")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminDiagnosticsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminDiagnosticsController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminErrorLogListItem>>> GetErrors(
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] string? traceId,
        [FromQuery] int? status,
        [FromQuery] string? path,
        [FromQuery] Guid? actorUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var validation = AdminValidation.ValidateAuditFilters(fromUtc, toUtc);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid diagnostics filters.").ApplyDefaults(HttpContext));
        }

        var query = _dbContext.ErrorLogs.AsNoTracking();

        if (fromUtc.HasValue)
        {
            query = query.Where(log => log.TimestampUtc >= fromUtc.Value.UtcDateTime);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(log => log.TimestampUtc <= toUtc.Value.UtcDateTime);
        }

        if (!string.IsNullOrWhiteSpace(traceId))
        {
            query = query.Where(log => log.TraceId.Contains(traceId));
        }

        if (status.HasValue)
        {
            query = query.Where(log => log.StatusCode == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            query = query.Where(log => log.Path.Contains(path));
        }

        if (actorUserId.HasValue)
        {
            query = query.Where(log => log.ActorUserId == actorUserId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderByDescending(log => log.TimestampUtc)
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

        var items = entries.Select(entry => new AdminErrorLogListItem(
            entry.Id,
            entry.TimestampUtc,
            entry.TraceId,
            entry.Path,
            entry.Method,
            entry.StatusCode,
            entry.Title,
            entry.Detail,
            entry.ActorUserId,
            entry.ActorUserId.HasValue && actorLookup.TryGetValue(entry.ActorUserId.Value, out var email)
                ? email
                : null)).ToList();

        return Ok(new PagedResult<AdminErrorLogListItem>(items, totalCount, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminErrorLogDetail>> GetError(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.ErrorLogs.AsNoTracking()
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        string? actorEmail = null;
        if (entry.ActorUserId.HasValue)
        {
            var actor = await _userManager.Users
                .Where(user => user.Id == entry.ActorUserId.Value)
                .Select(user => user.Email)
                .FirstOrDefaultAsync(cancellationToken);
            actorEmail = actor;
        }

        return Ok(new AdminErrorLogDetail(
            entry.Id,
            entry.TimestampUtc,
            entry.TraceId,
            entry.Path,
            entry.Method,
            entry.StatusCode,
            entry.Title,
            entry.Detail,
            entry.ExceptionType,
            entry.StackTrace,
            entry.InnerException,
            entry.DataJson,
            entry.ActorUserId,
            actorEmail,
            entry.UserAgent,
            entry.RemoteIp));
    }
}
