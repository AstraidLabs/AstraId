using AuthServer.Data;
using AuthServer.Options;
using AuthServer.Services;
using AuthServer.Services.Admin.Models;
using AuthServer.Services.Security;
using AuthServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthServer.Controllers.Admin;
/// <summary>
/// Exposes HTTP endpoints for admin diagnostics.
/// </summary>

[ApiController]
[Route("admin/api/diagnostics/errors")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminDiagnosticsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<SecurityDiagnosticsOptions> _securityDiagnosticsOptions;
    private readonly IOpenIddictClientSecretInspector _openIddictClientSecretInspector;

    public AdminDiagnosticsController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOptions<SecurityDiagnosticsOptions> securityDiagnosticsOptions,
        IOpenIddictClientSecretInspector openIddictClientSecretInspector)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _securityDiagnosticsOptions = securityDiagnosticsOptions;
        _openIddictClientSecretInspector = openIddictClientSecretInspector;
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

    [HttpGet("mfa-token-protection")]
    public async Task<IActionResult> GetMfaTokenProtectionStats(CancellationToken cancellationToken)
    {
        if (!_securityDiagnosticsOptions.Value.EnableMfaTokenProtectionEndpoint)
        {
            return NotFound();
        }

        const string provider = "[AspNetUserStore]";
        var tokens = await _dbContext.Set<IdentityUserToken<Guid>>()
            .AsNoTracking()
            .Where(item => item.LoginProvider == provider && (item.Name == "AuthenticatorKey" || item.Name == "RecoveryCodes"))
            .Select(item => item.Value)
            .ToListAsync(cancellationToken);

        var protectedCount = tokens.Count(value => !string.IsNullOrEmpty(value) && value.StartsWith("dpv1:", StringComparison.Ordinal));

        return Ok(new
        {
            total = tokens.Count,
            protectedCount,
            legacyOrUnknownCount = tokens.Count - protectedCount
        });
    }

    [HttpGet("openiddict-client-secret-storage")]
    public async Task<IActionResult> GetOpenIddictClientSecretStorage(CancellationToken cancellationToken)
    {
        if (!_securityDiagnosticsOptions.Value.EnableOpenIddictSecretStorageEndpoint)
        {
            return NotFound();
        }

        var (total, looksHashed, looksPlaintext) = await _openIddictClientSecretInspector.InspectAsync(cancellationToken);
        return Ok(new
        {
            total,
            looksHashed,
            looksPlaintext,
            allLookHashed = total == looksHashed
        });
    }
}
