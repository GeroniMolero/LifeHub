namespace LifeHub.Models
{
    public class DocumentVersion
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedByUserId { get; set; } = null!;

        public Document Document { get; set; } = null!;
        public ApplicationUser CreatedByUser { get; set; } = null!;
    }
}
