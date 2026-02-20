namespace AstraId.Logging.Audit;

/// <summary>
/// Defines the contract for security audit logger.
/// </summary>
public interface ISecurityAuditLogger
{
    void Log(SecurityAuditEvent securityEvent);
}
