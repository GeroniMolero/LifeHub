using System.ComponentModel.DataAnnotations;

namespace LifeHub.DTOs
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Claims { get; set; } = new();
    }

    public class PublicUserDto
    {
        public string Id { get; set; } = null!;
        public string? FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
    }

    public class UpdateProfileDto
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(2000)]
        public string? Bio { get; set; }

        [MaxLength(2000)]
        public string? ProfilePictureUrl { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = null!;

        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string NewPassword { get; set; } = null!;
    }
}
