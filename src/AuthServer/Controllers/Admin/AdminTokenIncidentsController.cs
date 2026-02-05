using AuthServer.Data;
using AuthServer.Services.Admin.Models;
using AuthServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/security/token-incidents")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminTokenIncidentsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminTokenIncidentsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminTokenIncidentListItem>>> GetIncidents(
        [FromQuery] string? type,
        [FromQuery] string? severity,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var validation = AdminValidation.ValidateAuditFilters(fromUtc, toUtc);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToProblemDetails("Invalid incident filters.").ApplyDefaults(HttpContext));
        }

        var query = _dbContext.TokenIncidents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(entry => entry.Type.Contains(type));
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            query = query.Where(entry => entry.Severity.Contains(severity));
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(entry => entry.TimestampUtc >= fromUtc.Value.UtcDateTime);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(entry => entry.TimestampUtc <= toUtc.Value.UtcDateTime);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(entry =>
                entry.Type.Contains(search)
                || entry.Severity.Contains(search)
                || (entry.ClientId != null && entry.ClientId.Contains(search))
                || (entry.TraceId != null && entry.TraceId.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderByDescending(entry => entry.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entries
            .Select(entry => new AdminTokenIncidentListItem(
                entry.Id,
                entry.TimestampUtc,
                entry.Type,
                entry.Severity,
                entry.UserId,
                entry.ClientId,
                entry.TraceId,
                entry.DetailJson))
            .ToList();

        return Ok(new PagedResult<AdminTokenIncidentListItem>(items, totalCount, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminTokenIncidentDetail>> GetIncident(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.TokenIncidents.AsNoTracking()
            .FirstOrDefaultAsync(incident => incident.Id == id, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        return Ok(new AdminTokenIncidentDetail(
            entry.Id,
            entry.TimestampUtc,
            entry.Type,
            entry.Severity,
            entry.UserId,
            entry.ClientId,
            entry.TraceId,
            entry.DetailJson,
            entry.ActorUserId));
    }
}
