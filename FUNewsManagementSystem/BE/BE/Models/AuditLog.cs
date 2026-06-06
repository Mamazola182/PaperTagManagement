using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAPI.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }
        [ForeignKey("AccountId")]
        public short UserId { get; set; } 
        public string? UserName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? BeforeData { get; set; }
        public string? AfterData { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
