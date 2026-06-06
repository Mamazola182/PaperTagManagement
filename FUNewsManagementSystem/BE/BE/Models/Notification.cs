using BE.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAPI.Models
{
    [Table("Notification")]
    public class Notification
    {
        [Key]
        [MaxLength(50)]
        public string NotificationId { get; set; } = null!;

        [Required]
        public short AccountId { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = null!; // new_article, comment, system, etc.

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = null!;

        [StringLength(20)]
        public string? RelatedId { get; set; } // Article ID, Comment ID, etc.

        [StringLength(50)]
        public string? RelatedType { get; set; } // article, comment, etc.

        public bool IsRead { get; set; } = false;

        public DateTime? CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [ForeignKey("AccountId")]
        public virtual SystemAccount Account { get; set; } = null!;
    }
}
