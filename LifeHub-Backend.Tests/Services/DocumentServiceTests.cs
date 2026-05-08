using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using LifeHub.Services.Documents;
using LifeHub.Tests.Helpers;

namespace LifeHub.Tests.Services
{
    public class DocumentServiceTests
    {
        private static DocumentService Create(LifeHub.Data.ApplicationDbContext ctx, int maxDocs = 5, int maxVersions = 10) =>
            new(ctx, TestHelpers.CreateMapper(), new PassThroughSanitizer(),
                TestHelpers.CreateOptions(r => { r.MaxDocumentsPerUser = maxDocs; r.MaxDocumentVersions = maxVersions; }));

        // ── GetDocumentsAsync ────────────────────────────────────────────────
        [Fact]
        public async Task GetDocuments_Owner_SeesOnlyOwnDocs()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            ctx.Documents.Add(new Document { UserId = "u1", Title = "Mine" });
            ctx.Documents.Add(new Document { UserId = "u2", Title = "Theirs" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetDocumentsAsync("u1", canViewAll: false);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("Mine", result.Value![0].Title);
        }

        [Fact]
        public async Task GetDocuments_Admin_ReturnsAll()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            ctx.Documents.Add(new Document { UserId = "u1", Title = "A" });
            ctx.Documents.Add(new Document { UserId = "u2", Title = "B" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetDocumentsAsync("u1", canViewAll: true);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        // ── GetDocumentAsync ─────────────────────────────────────────────────
        [Fact]
        public async Task GetDocument_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).GetDocumentAsync(99, "u1", canViewAll: false);

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetDocument_Owner_ReturnsDto()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            var doc = new Document { UserId = "u1", Title = "My Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetDocumentAsync(doc.Id, "u1", canViewAll: false);

            Assert.True(result.IsSuccess);
            Assert.Equal("My Doc", result.Value!.Title);
        }

        [Fact]
        public async Task GetDocument_WrongUser_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Secret" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetDocumentAsync(doc.Id, "u2", canViewAll: false);

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        // ── CreateDocumentAsync ──────────────────────────────────────────────
        [Fact]
        public async Task CreateDocument_InvalidUser_ReturnsUnauthorized()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).CreateDocumentAsync("ghost", new CreateDocumentDto { Title = "Doc" });

            Assert.Equal(ServiceResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public async Task CreateDocument_AtLimit_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            for (var i = 0; i < 5; i++)
                ctx.Documents.Add(new Document { UserId = "user1", Title = $"Doc {i}" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxDocs: 5).CreateDocumentAsync("user1",
                new CreateDocumentDto { Title = "New" });

            Assert.False(result.IsSuccess);
            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task CreateDocument_BelowLimit_Succeeds()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            for (var i = 0; i < 4; i++)
                ctx.Documents.Add(new Document { UserId = "user1", Title = $"Doc {i}" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxDocs: 5).CreateDocumentAsync("user1",
                new CreateDocumentDto { Title = "New" });

            Assert.True(result.IsSuccess);
        }

        // ── UpdateDocumentAsync ──────────────────────────────────────────────
        [Fact]
        public async Task UpdateDocument_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).UpdateDocumentAsync(99, "u1",
                new UpdateDocumentDto { Title = "X", Description = "", Content = "" });

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task UpdateDocument_NotOwner_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).UpdateDocumentAsync(doc.Id, "u2",
                new UpdateDocumentDto { Title = "X", Description = "", Content = "" });

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task UpdateDocument_AtVersionLimit_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            var user = TestHelpers.AddUser(ctx);

            var doc = new Document { UserId = "user1", Title = "Doc" };
            ctx.Documents.Add(doc);
            await ctx.SaveChangesAsync();

            for (var i = 1; i <= 3; i++)
                ctx.DocumentVersions.Add(new DocumentVersion { DocumentId = doc.Id, VersionNumber = i, Title = "v", CreatedByUserId = "user1" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxVersions: 3).UpdateDocumentAsync(doc.Id, "user1",
                new UpdateDocumentDto { Title = "Updated", Description = "", Content = "" });

            Assert.False(result.IsSuccess);
            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task UpdateDocument_Success_UpdatesTitleAndCreatesVersion()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            var doc = new Document { UserId = "u1", Title = "Old" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).UpdateDocumentAsync(doc.Id, "u1",
                new UpdateDocumentDto { Title = "New", Description = "", Content = "" });

            Assert.True(result.IsSuccess);
            Assert.Equal("New", result.Value!.Title);
            Assert.Equal(1, ctx.DocumentVersions.Count());
        }

        // ── DeleteDocumentAsync ──────────────────────────────────────────────
        [Fact]
        public async Task DeleteDocument_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).DeleteDocumentAsync(99, "u1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DeleteDocument_NotOwner_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteDocumentAsync(doc.Id, "u2");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task DeleteDocument_Success_Removes()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteDocumentAsync(doc.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal(0, ctx.Documents.Count());
        }
    }
}
