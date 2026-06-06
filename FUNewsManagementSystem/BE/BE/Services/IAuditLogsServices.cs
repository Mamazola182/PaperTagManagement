using CoreAPI.Models;
namespace CoreAPI.Services
{
    public interface IAuditLogsServices
    {
        public IQueryable<AuditLog> GetAuditLogs();
        public Task CreateAuditLog(AuditLog al);
    }
}
