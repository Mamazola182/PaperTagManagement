using CoreAPI.DataAccess;
using CoreAPI.Models;
namespace CoreAPI.Services
{
    public class AuditLogsServicesVer1: IAuditLogsServices
    {
        private readonly IAuditLogDA _auditLogDA;
        public AuditLogsServicesVer1(IAuditLogDA auditLogDA)
        {
            _auditLogDA = auditLogDA;
        }
        public IQueryable<AuditLog> GetAuditLogs()
        {
            return _auditLogDA.GetAuditLogs();
        }
        public async Task CreateAuditLog(AuditLog al)
        {
            await _auditLogDA.CreateAuditLog(al);
        }
    }
}
