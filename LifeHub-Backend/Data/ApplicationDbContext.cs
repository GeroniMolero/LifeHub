using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LifeHub.Models;

namespace LifeHub.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Recommendation> Recommendations { get; set; }
        public DbSet<RecommendationRating> RecommendationRatings { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<MusicFile> MusicFiles { get; set; }
        public DbSet<CreativeSpace> CreativeSpaces { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<DocumentPublication> DocumentPublications { get; set; }
        public DbSet<SpacePermission> SpacePermissions { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<AllowedWebsite> AllowedWebsites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============ FRIENDSHIPS ============
            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany(u => u.FriendshipsInitiated)
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Receiver)
                .WithMany(u => u.FriendshipsReceived)
                .HasForeignKey(f => f.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.RequesterId, f.ReceiverId })
                .IsUnique();

            // ============ MESSAGES ============
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.SenderId, m.ReceiverId });

            // ============ RECOMMENDATIONS ============
            modelBuilder.Entity<Recommendation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Recommendations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============ RECOMMENDATION RATINGS ============
            modelBuilder.Entity<RecommendationRating>()
                .HasOne(rr => rr.Recommendation)
                .WithMany(r => r.Ratings)
                .HasForeignKey(rr => rr.RecommendationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecommendationRating>()
                .HasOne(rr => rr.User)
                .WithMany(u => u.RecommendationRatings)
                .HasForeignKey(rr => rr.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RecommendationRating>()
                .HasIndex(rr => new { rr.RecommendationId, rr.UserId })
                .IsUnique();

            // ============ DOCUMENTS ============
            modelBuilder.Entity<Document>()
                .HasOne(d => d.User)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.CreativeSpace)
                .WithMany(cs => cs.Documents)
                .HasForeignKey(d => d.CreativeSpaceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.Publication)
                .WithOne(p => p.Document)
                .HasForeignKey<DocumentPublication>(p => p.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============ CREATIVE SPACES ============
            modelBuilder.Entity<CreativeSpace>()
                .HasOne(cs => cs.Owner)
                .WithMany(u => u.OwnedCreativeSpaces)
                .HasForeignKey(cs => cs.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CreativeSpace>()
                .HasIndex(cs => new { cs.OwnerId, cs.Name });

            // ============ DOCUMENT VERSIONS ============
            modelBuilder.Entity<DocumentVersion>()
                .HasOne(v => v.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentVersion>()
                .HasOne(v => v.CreatedByUser)
                .WithMany(u => u.DocumentVersions)
                .HasForeignKey(v => v.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DocumentVersion>()
                .HasIndex(v => new { v.DocumentId, v.VersionNumber })
                .IsUnique();

            // ============ DOCUMENT PUBLICATIONS ============
            modelBuilder.Entity<DocumentPublication>()
                .HasOne(p => p.PublishedByUser)
                .WithMany()
                .HasForeignKey(p => p.PublishedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DocumentPublication>()
                .HasIndex(p => p.DocumentId)
                .IsUnique();

            // ============ SPACE PERMISSIONS ============
            modelBuilder.Entity<SpacePermission>()
                .HasOne(p => p.CreativeSpace)
                .WithMany(cs => cs.Permissions)
                .HasForeignKey(p => p.CreativeSpaceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpacePermission>()
                .HasOne(p => p.User)
                .WithMany(u => u.SpacePermissions)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SpacePermission>()
                .HasOne(p => p.GrantedByUser)
                .WithMany(u => u.GrantedSpacePermissions)
                .HasForeignKey(p => p.GrantedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SpacePermission>()
                .HasIndex(p => new { p.CreativeSpaceId, p.UserId })
                .IsUnique();

            // ============ ACTIVITY LOGS ============
            modelBuilder.Entity<ActivityLog>()
                .HasOne(l => l.User)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(l => new { l.UserId, l.CreatedAt });

            // ============ MUSIC FILES ============
            modelBuilder.Entity<MusicFile>()
                .HasOne(m => m.User)
                .WithMany(u => u.MusicFiles)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============ ALLOWED WEBSITES ============
            modelBuilder.Entity<AllowedWebsite>()
                .HasIndex(w => w.Domain)
                .IsUnique();
        }
    }
}
