namespace LifeHub.Models
{
    public class CreativeSpace
    {
        public int Id { get; set; }
        public string OwnerId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public SpacePrivacy Privacy { get; set; } = SpacePrivacy.Private;
        public bool IsPublicProfileVisible { get; set; } = false;
        public string MediaReferencesJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser Owner { get; set; } = null!;
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<SpacePermission> Permissions { get; set; } = new List<SpacePermission>();
    }

    public enum SpacePrivacy
    {
        Private = 0,
        Shared = 1
    }
}
