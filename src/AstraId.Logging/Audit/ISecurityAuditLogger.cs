namespace AstraId.Logging.Audit;

public interface ISecurityAuditLogger
{
    void Log(SecurityAuditEvent securityEvent);
}
