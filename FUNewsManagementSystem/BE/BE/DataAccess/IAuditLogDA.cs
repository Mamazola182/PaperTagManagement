using BE.DTO;
using CoreAPI.Models;

namespace CoreAPI.DataAccess
{
    public interface IAuditLogDA
    {
        IQueryable<AuditLog> GetAuditLogs();
        Task CreateAuditLog(AuditLog al);
    }
}
