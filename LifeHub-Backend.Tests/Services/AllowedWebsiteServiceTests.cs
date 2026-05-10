using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using LifeHub.Services.AllowedWebsites;
using LifeHub.Tests.Helpers;

namespace LifeHub.Tests.Services
{
    public class AllowedWebsiteServiceTests
    {
        private static AllowedWebsiteService Create(LifeHub.Data.ApplicationDbContext ctx) =>
            new(ctx, TestHelpers.CreateMapper());

        [Fact]
        public async Task GetAllowedWebsites_ReturnsAll_OrderedByDomain()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "z.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "a.com", IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetAllowedWebsitesAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
            Assert.Equal("a.com", result.Value[0].Domain);
        }

        [Fact]
        public async Task GetActiveDomains_ReturnsOnlyActive()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "active.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "inactive.com", IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetActiveDomainsAsync();

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("active.com", result.Value![0]);
        }

        [Fact]
        public async Task CreateAllowedWebsite_EmptyDomain_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).CreateAllowedWebsiteAsync(new CreateAllowedWebsiteDto { Domain = "   " });

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task CreateAllowedWebsite_DuplicateDomain_ReturnsConflict()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "example.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).CreateAllowedWebsiteAsync(new CreateAllowedWebsiteDto { Domain = "example.com" });

            Assert.Equal(ServiceResultStatus.Conflict, result.Status);
        }

        [Fact]
        public async Task CreateAllowedWebsite_NormalizesHttpsAndWww()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).CreateAllowedWebsiteAsync(
                new CreateAllowedWebsiteDto { Domain = "https://www.example.com/path", IsActive = true });

            Assert.True(result.IsSuccess);
            Assert.Equal("example.com", result.Value!.Domain);
        }

        [Fact]
        public async Task CreateAllowedWebsite_Success_Persists()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).CreateAllowedWebsiteAsync(
                new CreateAllowedWebsiteDto { Domain = "example.com", IsActive = true });

            Assert.True(result.IsSuccess);
            Assert.Equal("example.com", result.Value!.Domain);
            Assert.Equal(1, ctx.AllowedWebsites.Count());
        }

        [Fact]
        public async Task UpdateAllowedWebsite_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).UpdateAllowedWebsiteAsync(99, new UpdateAllowedWebsiteDto { IsActive = false });

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task UpdateAllowedWebsite_Success_TogglesIsActive()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "example.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
            var id = ctx.AllowedWebsites.First().Id;

            var result = await Create(ctx).UpdateAllowedWebsiteAsync(id, new UpdateAllowedWebsiteDto { IsActive = false });

            Assert.True(result.IsSuccess);
            Assert.False(result.Value!.IsActive);
        }

        [Fact]
        public async Task DeleteAllowedWebsite_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).DeleteAllowedWebsiteAsync(99);

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DeleteAllowedWebsite_Success_Removes()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.AllowedWebsites.Add(new AllowedWebsite { Domain = "example.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
            var id = ctx.AllowedWebsites.First().Id;

            var result = await Create(ctx).DeleteAllowedWebsiteAsync(id);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, ctx.AllowedWebsites.Count());
        }
    }
}
