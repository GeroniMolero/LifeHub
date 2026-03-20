namespace LifeHub.DTOs
{
    public class DocumentVersionDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedByUserId { get; set; } = null!;
    }

    public class CreateDocumentVersionDto
    {
        public string? Note { get; set; }
    }
}
