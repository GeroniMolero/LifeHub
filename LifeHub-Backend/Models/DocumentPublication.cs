namespace LifeHub.Models
{
    public class DocumentPublication
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string? PublicTitle { get; set; }
        public string? PublicDescription { get; set; }
        public string MediaReferencesJson { get; set; } = "[]";
        public string ExternalLinksJson { get; set; } = "[]";
        public string PublishedByUserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Document Document { get; set; } = null!;
        public ApplicationUser PublishedByUser { get; set; } = null!;
    }
}
