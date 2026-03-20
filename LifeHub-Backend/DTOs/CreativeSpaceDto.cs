namespace LifeHub.DTOs
{
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
