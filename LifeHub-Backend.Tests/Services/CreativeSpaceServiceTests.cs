using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using LifeHub.Services.CreativeSpaces;
using LifeHub.Tests.Helpers;

namespace LifeHub.Tests.Services
{
    public class CreativeSpaceServiceTests
    {
        private static CreativeSpaceService Create(LifeHub.Data.ApplicationDbContext ctx, int maxSpaces = 10, int maxVisible = 5) =>
            new(ctx, TestHelpers.CreateMapper(), new NoOpActivityLogService(),
                TestHelpers.CreateOptions(r => { r.MaxSpacesPerUser = maxSpaces; r.MaxProfileVisibleSpacesPerUser = maxVisible; }));

        // ── GetCreativeSpacesAsync ───────────────────────────────────────────
        [Fact]
        public async Task GetCreativeSpaces_ReturnsOwned()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.CreativeSpaces.Add(new CreativeSpace { OwnerId = "u1", Name = "Mine" });
            ctx.CreativeSpaces.Add(new CreativeSpace { OwnerId = "u2", Name = "Theirs" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetCreativeSpacesAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("Mine", result.Value![0].Name);
        }

        [Fact]
        public async Task GetCreativeSpaces_IncludesShared()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u2", Name = "Shared" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();
            ctx.SpacePermissions.Add(new SpacePermission { CreativeSpaceId = space.Id, UserId = "u1", PermissionLevel = SpacePermissionLevel.Viewer, GrantedByUserId = "u2" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetCreativeSpacesAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }

        // ── GetPublicSpacesByUserAsync ────────────────────────────────────────
        [Fact]
        public async Task GetPublicSpacesByUser_ReturnsOnlyVisible()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.CreativeSpaces.Add(new CreativeSpace { OwnerId = "u1", Name = "Visible", IsPublicProfileVisible = true });
            ctx.CreativeSpaces.Add(new CreativeSpace { OwnerId = "u1", Name = "Hidden", IsPublicProfileVisible = false });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetPublicSpacesByUserAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("Visible", result.Value![0].Name);
        }

        // ── GetCreativeSpaceAsync ────────────────────────────────────────────
        [Fact]
        public async Task GetCreativeSpace_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).GetCreativeSpaceAsync(99, "u1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetCreativeSpace_NoAccess_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Private" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetCreativeSpaceAsync(space.Id, "u2");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task GetCreativeSpace_Owner_Succeeds()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "My Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetCreativeSpaceAsync(space.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal("My Space", result.Value!.Name);
        }

        // ── CreateCreativeSpaceAsync ─────────────────────────────────────────
        [Fact]
        public async Task CreateSpace_InvalidUser_ReturnsUnauthorized()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).CreateCreativeSpaceAsync("ghost",
                new CreateCreativeSpaceDto { Name = "X" }, "127.0.0.1");

            Assert.Equal(ServiceResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public async Task CreateSpace_AtLimit_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            for (var i = 0; i < 3; i++)
                ctx.CreativeSpaces.Add(new CreativeSpace { OwnerId = "user1", Name = $"Space {i}" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxSpaces: 3).CreateCreativeSpaceAsync("user1",
                new CreateCreativeSpaceDto { Name = "New Space" }, "127.0.0.1");

            Assert.False(result.IsSuccess);
            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task CreateSpace_BelowLimit_Succeeds()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            for (var i = 0; i < 2; i++)
                ctx.CreativeSpaces.Add(new CreativeSpace { OwnerId = "user1", Name = $"Space {i}" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxSpaces: 3).CreateCreativeSpaceAsync("user1",
                new CreateCreativeSpaceDto { Name = "New Space" }, "127.0.0.1");

            Assert.True(result.IsSuccess);
        }

        // ── UpdateCreativeSpaceAsync ─────────────────────────────────────────
        [Fact]
        public async Task UpdateSpace_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).UpdateCreativeSpaceAsync(99, "u1",
                new UpdateCreativeSpaceDto { Name = "X" }, "127.0.0.1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task UpdateSpace_MakeVisible_AtProfileLimit_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            for (var i = 0; i < 2; i++)
                ctx.CreativeSpaces.Add(new CreativeSpace { OwnerId = "user1", Name = $"Visible {i}", IsPublicProfileVisible = true });

            var target = new CreativeSpace { OwnerId = "user1", Name = "Target", IsPublicProfileVisible = false };
            ctx.CreativeSpaces.Add(target);
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxVisible: 2).UpdateCreativeSpaceAsync(target.Id, "user1",
                new UpdateCreativeSpaceDto { Name = "Target", IsPublicProfileVisible = true }, "127.0.0.1");

            Assert.False(result.IsSuccess);
            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task UpdateSpace_MakeVisible_BelowProfileLimit_Succeeds()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx);

            ctx.CreativeSpaces.Add(new CreativeSpace { OwnerId = "user1", Name = "Visible 1", IsPublicProfileVisible = true });

            var target = new CreativeSpace { OwnerId = "user1", Name = "Target", IsPublicProfileVisible = false };
            ctx.CreativeSpaces.Add(target);
            await ctx.SaveChangesAsync();

            var result = await Create(ctx, maxVisible: 2).UpdateCreativeSpaceAsync(target.Id, "user1",
                new UpdateCreativeSpaceDto { Name = "Target", IsPublicProfileVisible = true }, "127.0.0.1");

            Assert.True(result.IsSuccess);
        }

        // ── SetFavoriteAsync ─────────────────────────────────────────────────
        [Fact]
        public async Task SetFavorite_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).SetFavoriteAsync(99, "u1", true);

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task SetFavorite_SameValue_IsNoOp()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space", IsFavorite = true };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();
            var originalUpdatedAt = space.UpdatedAt;

            var result = await Create(ctx).SetFavoriteAsync(space.Id, "u1", true);

            Assert.True(result.IsSuccess);
            Assert.Equal(originalUpdatedAt, ctx.CreativeSpaces.First().UpdatedAt);
        }

        [Fact]
        public async Task SetFavorite_ChangesValue_UpdatesSpace()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space", IsFavorite = false };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).SetFavoriteAsync(space.Id, "u1", true);

            Assert.True(result.IsSuccess);
            Assert.True(ctx.CreativeSpaces.First().IsFavorite);
        }

        // ── GetPermissionsAsync ──────────────────────────────────────────────
        [Fact]
        public async Task GetPermissions_NotOwner_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetPermissionsAsync(space.Id, "u2");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task GetPermissions_Owner_ReturnsPermissions()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();
            ctx.SpacePermissions.Add(new SpacePermission { CreativeSpaceId = space.Id, UserId = "u2", GrantedByUserId = "u1" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetPermissionsAsync(space.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }

        // ── ShareAsync ───────────────────────────────────────────────────────
        [Fact]
        public async Task Share_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).ShareAsync(99, "u1",
                new ShareCreativeSpaceDto { UserId = "u2", PermissionLevel = 0 }, "127.0.0.1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task Share_TargetUserNotFound_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).ShareAsync(space.Id, "u1",
                new ShareCreativeSpaceDto { UserId = "ghost", PermissionLevel = 0 }, "127.0.0.1");

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task Share_NewPermission_CreatesRecord()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).ShareAsync(space.Id, "u1",
                new ShareCreativeSpaceDto { UserId = "u2", PermissionLevel = 0 }, "127.0.0.1");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, ctx.SpacePermissions.Count());
        }

        [Fact]
        public async Task Share_ExistingPermission_UpdatesLevel()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();
            ctx.SpacePermissions.Add(new SpacePermission { CreativeSpaceId = space.Id, UserId = "u2", GrantedByUserId = "u1", PermissionLevel = SpacePermissionLevel.Viewer });
            await ctx.SaveChangesAsync();

            await Create(ctx).ShareAsync(space.Id, "u1",
                new ShareCreativeSpaceDto { UserId = "u2", PermissionLevel = 1 }, "127.0.0.1");

            Assert.Equal(SpacePermissionLevel.Editor, ctx.SpacePermissions.First().PermissionLevel);
        }

        [Fact]
        public async Task Share_PrivateSpace_ChangesToShared()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space", Privacy = SpacePrivacy.Private };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            await Create(ctx).ShareAsync(space.Id, "u1",
                new ShareCreativeSpaceDto { UserId = "u2", PermissionLevel = 0 }, "127.0.0.1");

            Assert.Equal(SpacePrivacy.Shared, ctx.CreativeSpaces.First().Privacy);
        }

        // ── RemovePermissionAsync ────────────────────────────────────────────
        [Fact]
        public async Task RemovePermission_NotOwner_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).RemovePermissionAsync(space.Id, "u2", "u3", "127.0.0.1");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task RemovePermission_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).RemovePermissionAsync(space.Id, "u1", "ghost", "127.0.0.1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task RemovePermission_Success_DeletesRecord()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();
            ctx.SpacePermissions.Add(new SpacePermission { CreativeSpaceId = space.Id, UserId = "u2", GrantedByUserId = "u1" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).RemovePermissionAsync(space.Id, "u1", "u2", "127.0.0.1");

            Assert.True(result.IsSuccess);
            Assert.Equal(0, ctx.SpacePermissions.Count());
        }

        // ── GetMediaReferencesAsync ──────────────────────────────────────────
        [Fact]
        public async Task GetMediaReferences_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).GetMediaReferencesAsync(99, "u1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetMediaReferences_NoAccess_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetMediaReferencesAsync(space.Id, "u2");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task GetMediaReferences_Owner_ReturnsEmptyList()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space", MediaReferencesJson = "[]" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetMediaReferencesAsync(space.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        // ── AddMediaReferenceAsync ───────────────────────────────────────────
        [Fact]
        public async Task AddMediaReference_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).AddMediaReferenceAsync(99, "u1",
                new CreateSpaceMediaReferenceDto { EmbedUrl = "https://youtube.com/embed/1", Label = "L", Source = "S" });

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task AddMediaReference_NoAccess_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).AddMediaReferenceAsync(space.Id, "u2",
                new CreateSpaceMediaReferenceDto { EmbedUrl = "https://youtube.com/embed/1", Label = "L", Source = "S" });

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task AddMediaReference_InvalidUrl_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).AddMediaReferenceAsync(space.Id, "u1",
                new CreateSpaceMediaReferenceDto { EmbedUrl = "not-a-url", Label = "L", Source = "S" });

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task AddMediaReference_DomainNotAllowed_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "youtube.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).AddMediaReferenceAsync(space.Id, "u1",
                new CreateSpaceMediaReferenceDto { EmbedUrl = "https://notallowed.com/embed/1", Label = "L", Source = "S" });

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task AddMediaReference_Success_AppendsReference()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "youtube.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space", MediaReferencesJson = "[]" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).AddMediaReferenceAsync(space.Id, "u1",
                new CreateSpaceMediaReferenceDto { EmbedUrl = "https://youtube.com/embed/abc", Label = "My Video", Source = "YouTube" });

            Assert.True(result.IsSuccess);
            Assert.Equal("My Video", result.Value!.Label);
        }

        // ── RemoveMediaReferenceAsync ────────────────────────────────────────
        [Fact]
        public async Task RemoveMediaReference_SpaceNotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).RemoveMediaReferenceAsync(99, "u1", "ref-id");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task RemoveMediaReference_NoAccess_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space", MediaReferencesJson = "[]" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).RemoveMediaReferenceAsync(space.Id, "u2", "ref-id");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task RemoveMediaReference_RefNotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space", MediaReferencesJson = "[]" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).RemoveMediaReferenceAsync(space.Id, "u1", "nonexistent-id");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task RemoveMediaReference_Success_RemovesFromJson()
        {
            using var ctx = TestHelpers.CreateContext();
            const string refJson = """[{"Id":"abc123","Type":"external-embed","Label":"Video","Source":"YouTube","CreatedAt":"2025-01-01T00:00:00Z"}]""";
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space", MediaReferencesJson = refJson };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).RemoveMediaReferenceAsync(space.Id, "u1", "abc123");

            Assert.True(result.IsSuccess);
            Assert.Equal("[]", ctx.CreativeSpaces.First().MediaReferencesJson);
        }

        // ── DeleteCreativeSpaceAsync ─────────────────────────────────────────
        [Fact]
        public async Task DeleteCreativeSpace_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).DeleteCreativeSpaceAsync(99, "u1", "127.0.0.1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DeleteCreativeSpace_NotOwner_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteCreativeSpaceAsync(space.Id, "u2", "127.0.0.1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DeleteCreativeSpace_Success_Removes()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteCreativeSpaceAsync(space.Id, "u1", "127.0.0.1");

            Assert.True(result.IsSuccess);
            Assert.Equal(0, ctx.CreativeSpaces.Count());
        }

        [Fact]
        public async Task DeleteCreativeSpace_UnlinksDocuments()
        {
            using var ctx = TestHelpers.CreateContext();
            var space = new CreativeSpace { OwnerId = "u1", Name = "Space" };
            ctx.CreativeSpaces.Add(space); await ctx.SaveChangesAsync();
            ctx.Documents.Add(new Document { UserId = "u1", Title = "Doc", CreativeSpaceId = space.Id });
            await ctx.SaveChangesAsync();

            await Create(ctx).DeleteCreativeSpaceAsync(space.Id, "u1", "127.0.0.1");

            Assert.Null(ctx.Documents.First().CreativeSpaceId);
        }
    }
}
