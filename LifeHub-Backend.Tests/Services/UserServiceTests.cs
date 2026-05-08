using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using LifeHub.Services.Users;
using LifeHub.Tests.Helpers;

namespace LifeHub.Tests.Services
{
    public class UserServiceTests
    {
        private static UserService Create(LifeHub.Data.ApplicationDbContext ctx) =>
            new(TestHelpers.CreateUserManager(ctx), TestHelpers.CreateMapper());

        [Fact]
        public async Task GetUser_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).GetUserAsync("ghost");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetUser_Found_ReturnsDto()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).GetUserAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.Equal("u1", result.Value!.Id);
        }

        [Fact]
        public async Task GetCurrentUser_NotFound_ReturnsUnauthorized()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).GetCurrentUserAsync("ghost");

            Assert.Equal(ServiceResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public async Task GetCurrentUser_Found_ReturnsDto()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).GetCurrentUserAsync("u1");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetUsers_ReturnsAll()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            TestHelpers.AddUser(ctx, "u2");

            var result = await Create(ctx).GetUsersAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        [Fact]
        public async Task SearchUsers_ExcludesCurrentUser()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            TestHelpers.AddUser(ctx, "u2");

            var result = await Create(ctx).SearchUsersAsync("u1", null);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("u2", result.Value![0].Id);
        }

        [Fact]
        public async Task SearchUsers_FiltersByQuery_Email()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            TestHelpers.AddUser(ctx, "u2");

            var result = await Create(ctx).SearchUsersAsync("u1", "u2@test");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("u2", result.Value![0].Id);
        }

        [Fact]
        public async Task SearchUsers_NoMatch_ReturnsEmpty()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            TestHelpers.AddUser(ctx, "u2");

            var result = await Create(ctx).SearchUsersAsync("u1", "zzz-no-match");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        [Fact]
        public async Task UpdateProfile_NotFound_ReturnsUnauthorized()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).UpdateProfileAsync("ghost", new UpdateProfileDto { FullName = "X" });

            Assert.Equal(ServiceResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public async Task UpdateProfile_Success_UpdatesFields()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).UpdateProfileAsync("u1",
                new UpdateProfileDto { FullName = "John Doe", Bio = "Hello World" });

            Assert.True(result.IsSuccess);
            Assert.Equal("John Doe", result.Value!.FullName);
        }

        [Fact]
        public async Task DeleteUser_SelfDelete_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).DeleteUserAsync("u1", "u1");

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task DeleteUser_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).DeleteUserAsync("ghost", "u1");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DeleteUser_Success_Removes()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u2");

            var result = await Create(ctx).DeleteUserAsync("u2", "u1");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task DeleteCurrentUser_NotFound_ReturnsUnauthorized()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).DeleteCurrentUserAsync("ghost");

            Assert.Equal(ServiceResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public async Task DeleteCurrentUser_Success_DeletesAccount()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).DeleteCurrentUserAsync("u1");

            Assert.True(result.IsSuccess);
        }

        // ── ChangePasswordAsync ──────────────────────────────────────────────
        [Fact]
        public async Task ChangePassword_NotFound_ReturnsUnauthorized()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).ChangePasswordAsync("ghost",
                new ChangePasswordDto { CurrentPassword = "old", NewPassword = "New123!" });

            Assert.Equal(ServiceResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).ChangePasswordAsync("u1",
                new ChangePasswordDto { CurrentPassword = "wrong", NewPassword = "New123!" });

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }
    }
}
