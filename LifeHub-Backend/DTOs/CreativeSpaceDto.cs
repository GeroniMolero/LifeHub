using System.ComponentModel.DataAnnotations;

namespace LifeHub.DTOs
{
    public class SpaceMediaReferenceDto
    {
        public string Id { get; set; } = null!;
        public string Type { get; set; } = "external-embed";
        public string Label { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string? Provider { get; set; }
        public string? EmbedUrl { get; set; }
        public string? MimeType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CreateSpaceMediaReferenceDto
    {
        public string Label { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string? Provider { get; set; }
        public string EmbedUrl { get; set; } = null!;
    }

    public class CreativeSpaceDto
    {
        public int Id { get; set; }
        public string OwnerId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public int Privacy { get; set; }
        public bool IsPublicProfileVisible { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateCreativeSpaceDto
    {
        [Required]
        [MinLength(1)]
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public int Privacy { get; set; } = 0;
        public bool IsPublicProfileVisible { get; set; } = false;
    }

    public class UpdateCreativeSpaceDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public int Privacy { get; set; }
        public bool IsPublicProfileVisible { get; set; }
    }

    public class ShareCreativeSpaceDto
    {
        public string UserId { get; set; } = null!;
        public int PermissionLevel { get; set; } = 0;
    }

    public class SpacePermissionDto
    {
        public int Id { get; set; }
        public int CreativeSpaceId { get; set; }
        public string UserId { get; set; } = null!;
        public int PermissionLevel { get; set; }
        public string GrantedByUserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
