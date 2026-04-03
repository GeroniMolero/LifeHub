namespace LifeHub.Models
{
    /// <summary>
    /// Modelo para documentos (notas, archivos de texto, etc.)
    /// Solo almacena metadatos; el contenido se maneja en el cliente
    /// </summary>
    public class Document
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public int? CreativeSpaceId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // Contenido del documento
        public DocumentType Type { get; set; } // Note, TextFile, etc.
        public bool IsPublic { get; set; } = false;
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public ApplicationUser User { get; set; } = null!;
        public CreativeSpace? CreativeSpace { get; set; }
        public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
        public DocumentPublication? Publication { get; set; }
    }

    public enum DocumentType
    {
        Note = 0,
        TextFile = 1,
        List = 2
    }
}
