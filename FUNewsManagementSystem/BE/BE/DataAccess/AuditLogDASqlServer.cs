using CoreAPI.Models;
using Mscc.GenerativeAI;
using BE.Models;
namespace CoreAPI.DataAccess
{
    public class AuditLogDASqlServer:IAuditLogDA
    {
        private readonly FunewsManagementContext _context;
        public AuditLogDASqlServer(FunewsManagementContext context)
        {
            _context = context;
        }
        public IQueryable<AuditLog> GetAuditLogs()
        {
            return _context.AuditLogs;
        }
        public async Task CreateAuditLog(AuditLog al)
        {
            await _context.AuditLogs.AddAsync(al);
            await _context.SaveChangesAsync();
        }
    }
}
