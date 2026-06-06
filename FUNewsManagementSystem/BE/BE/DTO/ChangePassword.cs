using System.ComponentModel.DataAnnotations;

namespace BE.DTO
{
    public class ChangePassword
    {
        [Required]
        public string oldPassword { get; set; }
        [Required]
        public string newPassword { get; set; }
        [Required]
        public string confirmPassword { get; set; }
    }
}
