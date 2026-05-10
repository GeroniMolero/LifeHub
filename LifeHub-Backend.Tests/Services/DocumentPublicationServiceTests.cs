using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using LifeHub.Services.DocumentPublications;
using LifeHub.Tests.Helpers;

namespace LifeHub.Tests.Services
{
    public class DocumentPublicationServiceTests
    {
        private static DocumentPublicationService Create(LifeHub.Data.ApplicationDbContext ctx, int maxPublished = 10, int maxVisible = 5) =>
            new(ctx, TestHelpers.CreateOptions(r =>
            {
                r.MaxPublishedDocumentsPerUser = maxPublished;
                r.MaxProfileVisibleDocumentsPerUser = maxVisible;
            }));

        // ── GetPublicationAsync ──────────────────────────────────────────────
        [Fact]
        public async Task GetPublication_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).GetPublicationAsync(99, "u1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetPublication_NoPublicationRecord_ReturnsBasicDto()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc", IsPublic = false };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetPublicationAsync(doc.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal(doc.Id, result.Value!.DocumentId);
            Assert.False(result.Value.IsPublic);
        }

        [Fact]
        public async Task GetPublication_WithPublicationRecord_ReturnsFullDto()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc", IsPublic = true };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();
            ctx.DocumentPublications.Add(new DocumentPublication
            {
                DocumentId = doc.Id,
                PublishedByUserId = "u1",
                PublicTitle = "Public Title",
                IsProfileVisible = true
            });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetPublicationAsync(doc.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal("Public Title", result.Value!.PublicTitle);
            Assert.True(result.Value.IsProfileVisible);
        }

        // ── UpsertPublicationAsync ───────────────────────────────────────────
        [Fact]
        public async Task UpsertPublication_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).UpsertPublicationAsync(99, "u1",
                new UpsertDocumentPublicationDto { IsPublic = true });

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task UpsertPublication_Publish_AtLimit_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            for (var i = 0; i < 3; i++)
                ctx.Documents.Add(new Document { UserId = "user1", Title = $"Doc {i}", IsPublic = true });

            var target = new Document { UserId = "user1", Title = "Target", IsPublic = false };
            ctx.Documents.Add(target);
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxPublished: 3).UpsertPublicationAsync(target.Id, "user1",
                new UpsertDocumentPublicationDto { IsPublic = true });

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task UpsertPublication_Publish_BelowLimit_Succeeds()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            for (var i = 0; i < 2; i++)
                ctx.Documents.Add(new Document { UserId = "user1", Title = $"Doc {i}", IsPublic = true });

            var target = new Document { UserId = "user1", Title = "Target", IsPublic = false };
            ctx.Documents.Add(target);
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "example.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxPublished: 3).UpsertPublicationAsync(target.Id, "user1",
                new UpsertDocumentPublicationDto { IsPublic = true });

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task UpsertPublication_InvalidExternalLink_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).UpsertPublicationAsync(doc.Id, "u1",
                new UpsertDocumentPublicationDto
                {
                    IsPublic = false,
                    ExternalLinks = new List<string> { "https://notallowed.com/page" }
                });

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task UpsertPublication_AllowedExternalLink_Succeeds()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "example.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            var doc = new Document { UserId = "u1", Title = "Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).UpsertPublicationAsync(doc.Id, "u1",
                new UpsertDocumentPublicationDto
                {
                    IsPublic = false,
                    ExternalLinks = new List<string> { "https://example.com/page" }
                });

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task UpsertPublication_UpdatesExistingPublication()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc", IsPublic = true };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();
            ctx.DocumentPublications.Add(new DocumentPublication { DocumentId = doc.Id, PublishedByUserId = "u1", PublicTitle = "Old Title" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).UpsertPublicationAsync(doc.Id, "u1",
                new UpsertDocumentPublicationDto { IsPublic = true, PublicTitle = "New Title" });

            Assert.True(result.IsSuccess);
            Assert.Equal("New Title", result.Value!.PublicTitle);
            Assert.Equal(1, ctx.DocumentPublications.Count());
        }

        // ── GetPublicDocumentAsync ───────────────────────────────────────────
        [Fact]
        public async Task GetPublicDocument_NotPublic_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Private", IsPublic = false };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetPublicDocumentAsync(doc.Id);

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetPublicDocument_NoPublication_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc", IsPublic = true };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetPublicDocumentAsync(doc.Id);

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetPublicDocument_Found_ReturnsView()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "My Doc", Content = "Hello", IsPublic = true };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();
            ctx.DocumentPublications.Add(new DocumentPublication
            {
                DocumentId = doc.Id,
                PublishedByUserId = "u1",
                PublicTitle = "Public Title",
                Author = "The Author"
            });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetPublicDocumentAsync(doc.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal("Public Title", result.Value!.Title);
            Assert.Equal("The Author", result.Value.Author);
            Assert.Equal("Hello", result.Value.Content);
        }

        // ── SetProfileVisibilityAsync ────────────────────────────────────────
        [Fact]
        public async Task SetProfileVisibility_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).SetProfileVisibilityAsync(99, "u1", true);

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task SetProfileVisibility_AtLimit_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            for (var i = 0; i < 2; i++)
            {
                var d = new Document { UserId = "user1", Title = $"Doc {i}", IsPublic = true };
                ctx.Documents.Add(d);
                await ctx.SaveChangesAsync();
                ctx.DocumentPublications.Add(new DocumentPublication { DocumentId = d.Id, PublishedByUserId = "user1", IsProfileVisible = true });
            }

            var target = new Document { UserId = "user1", Title = "Target", IsPublic = true };
            ctx.Documents.Add(target);
            await ctx.SaveChangesAsync();
            ctx.DocumentPublications.Add(new DocumentPublication { DocumentId = target.Id, PublishedByUserId = "user1", IsProfileVisible = false });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxVisible: 2).SetProfileVisibilityAsync(target.Id, "user1", true);

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task SetProfileVisibility_BelowLimit_Succeeds()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            var doc1 = new Document { UserId = "user1", Title = "Doc 1", IsPublic = true };
            ctx.Documents.Add(doc1);
            await ctx.SaveChangesAsync();
            ctx.DocumentPublications.Add(new DocumentPublication { DocumentId = doc1.Id, PublishedByUserId = "user1", IsProfileVisible = true });

            var target = new Document { UserId = "user1", Title = "Target", IsPublic = true };
            ctx.Documents.Add(target);
            await ctx.SaveChangesAsync();
            ctx.DocumentPublications.Add(new DocumentPublication { DocumentId = target.Id, PublishedByUserId = "user1", IsProfileVisible = false });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxVisible: 2).SetProfileVisibilityAsync(target.Id, "user1", true);

            Assert.True(result.IsSuccess);
        }
    }
}
