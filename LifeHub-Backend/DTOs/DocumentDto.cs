namespace LifeHub.DTOs
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string? CreatorName { get; set; }
        public int? CreativeSpaceId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int Type { get; set; } // DocumentType as int
        public bool IsPublic { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateDocumentDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Type { get; set; }
        public int? CreativeSpaceId { get; set; }
    }

    public class UpdateDocumentDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int? CreativeSpaceId { get; set; }
    }
}
