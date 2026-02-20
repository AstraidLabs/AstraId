using System.Diagnostics;
using AstraId.Logging.Options;
using AstraId.Logging.Redaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AstraId.Logging.Audit;

/// <summary>
/// Provides security audit logger functionality.
/// </summary>
public sealed class SecurityAuditLogger : ISecurityAuditLogger
{
    private readonly ILogger _logger;
    private readonly IOptions<AstraLoggingOptions> _options;

    public SecurityAuditLogger(ILoggerFactory loggerFactory, IOptions<AstraLoggingOptions> options)
    {
        _logger = loggerFactory.CreateLogger("AstraId.Logging.SecurityAudit");
        _options = options;
    }

    public void Log(SecurityAuditEvent securityEvent)
    {
        var opts = _options.Value;
        if (!opts.SecurityAudit.Enabled)
        {
            return;
        }

        securityEvent.TimestampUtc = securityEvent.TimestampUtc == default ? DateTimeOffset.UtcNow : securityEvent.TimestampUtc;
        securityEvent.TraceId ??= Activity.Current?.Id;

        _logger.Log(opts.SecurityAudit.MinimumLevel,
            "SECURITY_AUDIT eventType={EventType} timestampUtc={TimestampUtc} service={Service} environment={Environment} actorType={ActorType} actorId={ActorId} target={Target} action={Action} result={Result} reasonCode={ReasonCode} correlationId={CorrelationId} traceId={TraceId} ip={Ip} userAgentHash={UserAgentHash}",
            securityEvent.EventType,
            securityEvent.TimestampUtc,
            securityEvent.Service,
            securityEvent.Environment,
            securityEvent.ActorType,
            LogSanitizer.ComputeStableHash(securityEvent.ActorId),
            securityEvent.Target,
            securityEvent.Action,
            securityEvent.Result,
            securityEvent.ReasonCode,
            securityEvent.CorrelationId,
            securityEvent.TraceId,
            securityEvent.Ip,
            securityEvent.UserAgentHash);
    }
}
