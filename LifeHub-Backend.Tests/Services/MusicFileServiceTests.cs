using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using LifeHub.Services.MusicFiles;
using LifeHub.Tests.Helpers;

namespace LifeHub.Tests.Services
{
    public class MusicFileServiceTests
    {
        private static MusicFileService Create(LifeHub.Data.ApplicationDbContext ctx) =>
            new(ctx, TestHelpers.CreateMapper());

        [Fact]
        public async Task GetMusicFiles_ReturnsOnlyOwnerFiles()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.MusicFiles.Add(new MusicFile { UserId = "u1", FileName = "a.mp3", Title = "A" });
            ctx.MusicFiles.Add(new MusicFile { UserId = "u2", FileName = "b.mp3", Title = "B" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetMusicFilesAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }

        [Fact]
        public async Task GetMusicFile_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var result = await Create(ctx).GetMusicFileAsync(99, "u1");
            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetMusicFile_WrongOwner_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var f = new MusicFile { UserId = "u1", FileName = "a.mp3", Title = "A" };
            ctx.MusicFiles.Add(f); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetMusicFileAsync(f.Id, "u2");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetMusicFile_Found_ReturnsDto()
        {
            using var ctx = TestHelpers.CreateContext();
            var f = new MusicFile { UserId = "u1", FileName = "a.mp3", Title = "Song" };
            ctx.MusicFiles.Add(f); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetMusicFileAsync(f.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal("Song", result.Value!.Title);
        }

        [Fact]
        public async Task CreateMusicFile_InvalidUser_ReturnsUnauthorized()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).CreateMusicFileAsync("ghost",
                new CreateMusicFileDto { FileName = "a.mp3", Title = "A" });

            Assert.Equal(ServiceResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public async Task CreateMusicFile_Success_Persists()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).CreateMusicFileAsync("u1",
                new CreateMusicFileDto { FileName = "track.mp3", Title = "Track" });

            Assert.True(result.IsSuccess);
            Assert.Equal("Track", result.Value!.Title);
            Assert.Equal(1, ctx.MusicFiles.Count());
        }

        [Fact]
        public async Task UpdateMusicFile_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).UpdateMusicFileAsync(99, "u1",
                new UpdateMusicFileDto { Title = "X" });

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task UpdateMusicFile_Success_UpdatesFields()
        {
            using var ctx = TestHelpers.CreateContext();
            var f = new MusicFile { UserId = "u1", FileName = "a.mp3", Title = "Old" };
            ctx.MusicFiles.Add(f); await ctx.SaveChangesAsync();

            var result = await Create(ctx).UpdateMusicFileAsync(f.Id, "u1",
                new UpdateMusicFileDto { Title = "New" });

            Assert.True(result.IsSuccess);
            Assert.Equal("New", result.Value!.Title);
        }

        [Fact]
        public async Task DeleteMusicFile_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var result = await Create(ctx).DeleteMusicFileAsync(99, "u1");
            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DeleteMusicFile_Success_Removes()
        {
            using var ctx = TestHelpers.CreateContext();
            var f = new MusicFile { UserId = "u1", FileName = "a.mp3", Title = "A" };
            ctx.MusicFiles.Add(f); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteMusicFileAsync(f.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal(0, ctx.MusicFiles.Count());
        }
    }
}
