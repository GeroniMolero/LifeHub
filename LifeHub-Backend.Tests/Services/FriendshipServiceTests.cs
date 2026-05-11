using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using LifeHub.Services.Friendships;
using LifeHub.Tests.Helpers;

namespace LifeHub.Tests.Services
{
    public class FriendshipServiceTests
    {
        private static FriendshipService Create(LifeHub.Data.ApplicationDbContext ctx) =>
            new(ctx, TestHelpers.CreateMapper());

        // ── GetFriendshipsAsync ──────────────────────────────────────────────
        [Fact]
        public async Task GetFriendships_ReturnsAll_ForUser()
        {
            using var ctx = TestHelpers.CreateContext();
            var u1 = TestHelpers.AddUser(ctx, "u1");
            var u2 = TestHelpers.AddUser(ctx, "u2");
            ctx.Friendships.Add(new Friendship { RequesterId = "u1", ReceiverId = "u2" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetFriendshipsAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }

        [Fact]
        public async Task GetFriendships_Empty_WhenNoRelations()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).GetFriendshipsAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        // ── GetAcceptedFriendsAsync ──────────────────────────────────────────
        [Fact]
        public async Task GetAcceptedFriends_FiltersOnlyAccepted()
        {
            using var ctx = TestHelpers.CreateContext();
            var u1 = TestHelpers.AddUser(ctx, "u1");
            var u2 = TestHelpers.AddUser(ctx, "u2");
            var u3 = TestHelpers.AddUser(ctx, "u3");
            ctx.Friendships.Add(new Friendship { RequesterId = "u1", ReceiverId = "u2", Status = FriendshipStatus.Accepted });
            ctx.Friendships.Add(new Friendship { RequesterId = "u1", ReceiverId = "u3", Status = FriendshipStatus.Pending });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetAcceptedFriendsAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }

        // ── SendFriendRequestAsync ───────────────────────────────────────────
        [Fact]
        public async Task SendFriendRequest_InvalidUser_ReturnsUnauthorized()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).SendFriendRequestAsync("ghost", new CreateFriendshipDto { ReceiverId = "u2" });

            Assert.Equal(ServiceResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public async Task SendFriendRequest_ToSelf_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).SendFriendRequestAsync("u1", new CreateFriendshipDto { ReceiverId = "u1" });

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task SendFriendRequest_ReceiverNotFound_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).SendFriendRequestAsync("u1", new CreateFriendshipDto { ReceiverId = "u2" });

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task SendFriendRequest_AlreadyExists_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            TestHelpers.AddUser(ctx, "u2");
            ctx.Friendships.Add(new Friendship { RequesterId = "u1", ReceiverId = "u2" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).SendFriendRequestAsync("u1", new CreateFriendshipDto { ReceiverId = "u2" });

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task SendFriendRequest_Success_CreatesPending()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            TestHelpers.AddUser(ctx, "u2");

            var result = await Create(ctx).SendFriendRequestAsync("u1", new CreateFriendshipDto { ReceiverId = "u2" });

            Assert.True(result.IsSuccess);
            Assert.Equal("u1", result.Value!.RequesterId);
        }

        // ── UpdateFriendshipAsync ────────────────────────────────────────────
        [Fact]
        public async Task UpdateFriendship_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).UpdateFriendshipAsync(99, "u2", new UpdateFriendshipDto { Status = 1 });

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task UpdateFriendship_NotReceiver_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            var f = new Friendship { RequesterId = "u1", ReceiverId = "u2" };
            ctx.Friendships.Add(f); await ctx.SaveChangesAsync();

            var result = await Create(ctx).UpdateFriendshipAsync(f.Id, "u1", new UpdateFriendshipDto { Status = 1 });

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task UpdateFriendship_Success_ChangesStatus()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            var f = new Friendship { RequesterId = "u1", ReceiverId = "u2" };
            ctx.Friendships.Add(f); await ctx.SaveChangesAsync();

            var result = await Create(ctx).UpdateFriendshipAsync(f.Id, "u2", new UpdateFriendshipDto { Status = 1 });

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value!.Status);
        }

        // ── DeleteFriendshipAsync ────────────────────────────────────────────
        [Fact]
        public async Task DeleteFriendship_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).DeleteFriendshipAsync(99, "u1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DeleteFriendship_UnrelatedUser_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            var f = new Friendship { RequesterId = "u1", ReceiverId = "u2" };
            ctx.Friendships.Add(f); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteFriendshipAsync(f.Id, "u3");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task DeleteFriendship_Success_RemovesRecord()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            var f = new Friendship { RequesterId = "u1", ReceiverId = "u2" };
            ctx.Friendships.Add(f); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteFriendshipAsync(f.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal(0, ctx.Friendships.Count());
        }
    }
}
