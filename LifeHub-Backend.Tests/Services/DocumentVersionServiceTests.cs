using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using LifeHub.Services.DocumentVersions;
using LifeHub.Tests.Helpers;

namespace LifeHub.Tests.Services
{
    public class DocumentVersionServiceTests
    {
        private static DocumentVersionService Create(LifeHub.Data.ApplicationDbContext ctx, int maxVersions = 10) =>
            new(ctx, TestHelpers.CreateMapper(), new NoOpActivityLogService(),
                TestHelpers.CreateOptions(r => r.MaxDocumentVersions = maxVersions));

        // ── GetDocumentVersionsAsync ─────────────────────────────────────────
        [Fact]
        public async Task GetDocumentVersions_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).GetDocumentVersionsAsync(99, "u1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetDocumentVersions_NoAccess_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetDocumentVersionsAsync(doc.Id, "u2");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task GetDocumentVersions_Owner_ReturnsVersions()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            var doc = new Document { UserId = "u1", Title = "Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();
            ctx.DocumentVersions.Add(new DocumentVersion { DocumentId = doc.Id, VersionNumber = 1, Title = "v1", CreatedByUserId = "u1" });
            ctx.DocumentVersions.Add(new DocumentVersion { DocumentId = doc.Id, VersionNumber = 2, Title = "v2", CreatedByUserId = "u1" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetDocumentVersionsAsync(doc.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        // ── CreateSnapshotAsync ──────────────────────────────────────────────
        [Fact]
        public async Task CreateSnapshot_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).CreateSnapshotAsync(99, "u1",
                new CreateDocumentVersionDto { Note = "snap" }, "127.0.0.1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task CreateSnapshot_NoAccess_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();

            var result = await Create(ctx).CreateSnapshotAsync(doc.Id, "u2",
                new CreateDocumentVersionDto { Note = "snap" }, "127.0.0.1");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task CreateSnapshot_AtVersionLimit_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            var doc = new Document { UserId = "user1", Title = "Doc" };
            ctx.Documents.Add(doc);
            await ctx.SaveChangesAsync();

            for (var i = 1; i <= 3; i++)
                ctx.DocumentVersions.Add(new DocumentVersion { DocumentId = doc.Id, VersionNumber = i, Title = "v", CreatedByUserId = "user1" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxVersions: 3).CreateSnapshotAsync(doc.Id, "user1",
                new CreateDocumentVersionDto { Note = "snap" }, "127.0.0.1");

            Assert.False(result.IsSuccess);
            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task CreateSnapshot_BelowVersionLimit_Succeeds()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            var doc = new Document { UserId = "user1", Title = "Doc" };
            ctx.Documents.Add(doc);
            await ctx.SaveChangesAsync();

            for (var i = 1; i <= 2; i++)
                ctx.DocumentVersions.Add(new DocumentVersion { DocumentId = doc.Id, VersionNumber = i, Title = "v", CreatedByUserId = "user1" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxVersions: 3).CreateSnapshotAsync(doc.Id, "user1",
                new CreateDocumentVersionDto { Note = "snap" }, "127.0.0.1");

            Assert.True(result.IsSuccess);
        }

        // ── RestoreVersionAsync ──────────────────────────────────────────────
        [Fact]
        public async Task RestoreVersion_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).RestoreVersionAsync(99, "u1", "127.0.0.1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task RestoreVersion_NoAccess_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc", Content = "Original" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();
            var ver = new DocumentVersion { DocumentId = doc.Id, VersionNumber = 1, Title = "v1", Content = "Old", CreatedByUserId = "u1" };
            ctx.DocumentVersions.Add(ver); await ctx.SaveChangesAsync();

            var result = await Create(ctx).RestoreVersionAsync(ver.Id, "u2", "127.0.0.1");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task RestoreVersion_Success_UpdatesDocument()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Current", Content = "New content" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();
            var ver = new DocumentVersion { DocumentId = doc.Id, VersionNumber = 1, Title = "OldTitle", Content = "OldContent", CreatedByUserId = "u1" };
            ctx.DocumentVersions.Add(ver); await ctx.SaveChangesAsync();

            var result = await Create(ctx).RestoreVersionAsync(ver.Id, "u1", "127.0.0.1");

            Assert.True(result.IsSuccess);
            Assert.Equal(doc.Id, result.Value!.DocumentId);
            Assert.Equal("OldTitle", ctx.Documents.First().Title);
        }

        // ── DeleteDocumentVersionAsync ───────────────────────────────────────
        [Fact]
        public async Task DeleteDocumentVersion_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).DeleteDocumentVersionAsync(99, "u1", "127.0.0.1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DeleteDocumentVersion_NotOwner_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();
            var ver = new DocumentVersion { DocumentId = doc.Id, VersionNumber = 1, Title = "v1", CreatedByUserId = "u1" };
            ctx.DocumentVersions.Add(ver); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteDocumentVersionAsync(ver.Id, "u2", "127.0.0.1");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task DeleteDocumentVersion_Success_Removes()
        {
            using var ctx = TestHelpers.CreateContext();
            var doc = new Document { UserId = "u1", Title = "Doc" };
            ctx.Documents.Add(doc); await ctx.SaveChangesAsync();
            var ver = new DocumentVersion { DocumentId = doc.Id, VersionNumber = 1, Title = "v1", CreatedByUserId = "u1" };
            ctx.DocumentVersions.Add(ver); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteDocumentVersionAsync(ver.Id, "u1", "127.0.0.1");

            Assert.True(result.IsSuccess);
            Assert.Equal(0, ctx.DocumentVersions.Count());
        }
    }
}
