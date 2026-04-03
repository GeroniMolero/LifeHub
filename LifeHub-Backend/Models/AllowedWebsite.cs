namespace LifeHub.Models
{
    public class AllowedWebsite
    {
        public int Id { get; set; }
        public string Domain { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
