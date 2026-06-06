using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using BE.Models;
namespace BE.DTO
{
    public class ActiveNews
    {
        [Key]
        public string? NewsArticleId { get; set; } = null!;
        public string? NewsTitle { get; set; }
        public string Headline { get; set; } = null!;
        public DateTime? CreatedDate { get; set; }
        public string? NewsContent { get; set; }
        public string? NewsSource { get; set; }
        public bool? NewsStatus { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public Category? Category { get; set; }
        public SystemAccount? CreatedBy { get; set; }
        public SystemAccount? UpdatedBy { get; set; }
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
