using System.ComponentModel.DataAnnotations;

namespace LifeHub.DTOs
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [MinLength(10)]
        [MaxLength(128)]
        public string Password { get; set; } = null!;

        [Required]
        public string ConfirmPassword { get; set; } = null!;
    }
}
