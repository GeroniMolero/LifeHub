namespace LifeHub.Models
{
    public class SpacePermission
    {
        public int Id { get; set; }
        public int CreativeSpaceId { get; set; }
        public string UserId { get; set; } = null!;
        public SpacePermissionLevel PermissionLevel { get; set; } = SpacePermissionLevel.Viewer;
        public string GrantedByUserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public CreativeSpace CreativeSpace { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public ApplicationUser GrantedByUser { get; set; } = null!;
    }

    public enum SpacePermissionLevel
    {
        Viewer = 0,
        Editor = 1
    }
}
