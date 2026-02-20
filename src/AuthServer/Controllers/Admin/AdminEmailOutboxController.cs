using AuthServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers.Admin;
/// <summary>
/// Exposes HTTP endpoints for admin email outbox.
/// </summary>

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("admin/api/diagnostics/email-outbox")]
public sealed class AdminEmailOutboxController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminEmailOutboxController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? type, [FromQuery] Guid? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var query = _db.EmailOutboxMessages.AsNoTracking();
        if (Enum.TryParse<EmailOutboxStatus>(status, true, out var parsedStatus)) query = query.Where(x => x.Status == parsedStatus);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.Type == type);
        if (userId.HasValue) query = query.Where(x => x.UserId == userId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedUtc)
            .Skip((Math.Max(1, page) - 1) * Math.Max(1, pageSize))
            .Take(Math.Max(1, pageSize))
            .ToListAsync(cancellationToken);

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var item = await _db.EmailOutboxMessages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();
        item.Status = EmailOutboxStatus.Pending;
        item.NextAttemptUtc = DateTime.UtcNow;
        item.Error = null;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var item = await _db.EmailOutboxMessages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();
        item.Status = EmailOutboxStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
