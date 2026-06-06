using System.ComponentModel.DataAnnotations;

namespace BE.DTO
{
    public class Login
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
