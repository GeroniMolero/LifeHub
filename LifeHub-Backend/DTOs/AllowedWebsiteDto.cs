using System.ComponentModel.DataAnnotations;

namespace LifeHub.DTOs
{
    public class AllowedWebsiteDto
    {
        public int Id { get; set; }
        public string Domain { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateAllowedWebsiteDto
    {
        [Required]
        [MinLength(3)]
        [MaxLength(200)]
        public string Domain { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateAllowedWebsiteDto
    {
        public bool IsActive { get; set; }
    }
}
