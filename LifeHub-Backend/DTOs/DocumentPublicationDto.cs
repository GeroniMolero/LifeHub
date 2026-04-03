namespace LifeHub.DTOs
{
    public class MediaReferenceDto
    {
        public string Type { get; set; } = null!;
        public string Label { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string? Provider { get; set; }
        public string? EmbedUrl { get; set; }
    }

    public class DocumentPublicationDto
    {
        public int DocumentId { get; set; }
        public bool IsPublic { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? PublicTitle { get; set; }
        public string? PublicDescription { get; set; }
        public List<MediaReferenceDto> MediaReferences { get; set; } = new();
        public List<string> ExternalLinks { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpsertDocumentPublicationDto
    {
        public bool IsPublic { get; set; }
        public string? PublicTitle { get; set; }
        public string? PublicDescription { get; set; }
        public List<MediaReferenceDto> MediaReferences { get; set; } = new();
        public List<string> ExternalLinks { get; set; } = new();
    }

    public class PublicDocumentViewDto
    {
        public int DocumentId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public List<MediaReferenceDto> MediaReferences { get; set; } = new();
        public List<string> ExternalLinks { get; set; } = new();
    }
}
