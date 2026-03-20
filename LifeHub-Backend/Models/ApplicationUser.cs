using Microsoft.AspNetCore.Identity;

namespace LifeHub.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public ICollection<Friendship> FriendshipsInitiated { get; set; } = new List<Friendship>();
        public ICollection<Friendship> FriendshipsReceived { get; set; } = new List<Friendship>();
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
        public ICollection<RecommendationRating> RecommendationRatings { get; set; } = new List<RecommendationRating>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<MusicFile> MusicFiles { get; set; } = new List<MusicFile>();
        public ICollection<CreativeSpace> OwnedCreativeSpaces { get; set; } = new List<CreativeSpace>();
        public ICollection<SpacePermission> SpacePermissions { get; set; } = new List<SpacePermission>();
        public ICollection<SpacePermission> GrantedSpacePermissions { get; set; } = new List<SpacePermission>();
        public ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();
        public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    }
}
