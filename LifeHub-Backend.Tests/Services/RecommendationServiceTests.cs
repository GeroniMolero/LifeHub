using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using LifeHub.Services.Recommendations;
using LifeHub.Tests.Helpers;

namespace LifeHub.Tests.Services
{
    public class RecommendationServiceTests
    {
        private static RecommendationService Create(LifeHub.Data.ApplicationDbContext ctx) =>
            new(ctx, TestHelpers.CreateMapper());

        // ── GetRecommendationsAsync ──────────────────────────────────────────
        [Fact]
        public async Task GetRecommendations_ReturnsAll()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            ctx.Recommendations.Add(new Recommendation { UserId = "u1", Title = "A", Description = "d", Type = RecommendationType.Movie });
            ctx.Recommendations.Add(new Recommendation { UserId = "u1", Title = "B", Description = "d", Type = RecommendationType.Book });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetRecommendationsAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        // ── GetRecommendationAsync ───────────────────────────────────────────
        [Fact]
        public async Task GetRecommendation_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).GetRecommendationAsync(99);

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetRecommendation_Found_ReturnsDto()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            var rec = new Recommendation { UserId = "u1", Title = "Movie1", Description = "d", Type = RecommendationType.Movie };
            ctx.Recommendations.Add(rec); await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetRecommendationAsync(rec.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal("Movie1", result.Value!.Title);
        }

        // ── GetUserRecommendationsAsync ──────────────────────────────────────
        [Fact]
        public async Task GetUserRecommendations_FiltersToUser()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            ctx.Recommendations.Add(new Recommendation { UserId = "u1", Title = "A", Description = "d", Type = RecommendationType.Movie });
            ctx.Recommendations.Add(new Recommendation { UserId = "u2", Title = "B", Description = "d", Type = RecommendationType.Book });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetUserRecommendationsAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("A", result.Value![0].Title);
        }

        // ── CreateRecommendationAsync ────────────────────────────────────────
        [Fact]
        public async Task CreateRecommendation_InvalidUser_ReturnsUnauthorized()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).CreateRecommendationAsync("ghost",
                new RecommendationFormDto { Title = "T", Description = "d", Type = 0 });

            Assert.Equal(ServiceResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public async Task CreateRecommendation_Success_Persists()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).CreateRecommendationAsync("u1",
                new RecommendationFormDto { Title = "Inception", Description = "great", Type = 0 });

            Assert.True(result.IsSuccess);
            Assert.Equal("Inception", result.Value!.Title);
            Assert.Equal(1, ctx.Recommendations.Count());
        }

        // ── UpdateRecommendationAsync ────────────────────────────────────────
        [Fact]
        public async Task UpdateRecommendation_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).UpdateRecommendationAsync(99, "u1",
                new RecommendationFormDto { Title = "T", Description = "d", Type = 0 });

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task UpdateRecommendation_NotOwner_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            var rec = new Recommendation { UserId = "u1", Title = "T", Description = "d", Type = RecommendationType.Movie };
            ctx.Recommendations.Add(rec); await ctx.SaveChangesAsync();

            var result = await Create(ctx).UpdateRecommendationAsync(rec.Id, "u2",
                new RecommendationFormDto { Title = "X", Description = "d", Type = 0 });

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task UpdateRecommendation_Success_UpdatesFields()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            var rec = new Recommendation { UserId = "u1", Title = "Old", Description = "d", Type = RecommendationType.Movie };
            ctx.Recommendations.Add(rec); await ctx.SaveChangesAsync();

            var result = await Create(ctx).UpdateRecommendationAsync(rec.Id, "u1",
                new RecommendationFormDto { Title = "New", Description = "d2", Type = 0 });

            Assert.True(result.IsSuccess);
            Assert.Equal("New", result.Value!.Title);
        }

        // ── DeleteRecommendationAsync ────────────────────────────────────────
        [Fact]
        public async Task DeleteRecommendation_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var result = await Create(ctx).DeleteRecommendationAsync(99, "u1");
            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DeleteRecommendation_NotOwner_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            var rec = new Recommendation { UserId = "u1", Title = "T", Description = "d", Type = RecommendationType.Movie };
            ctx.Recommendations.Add(rec); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteRecommendationAsync(rec.Id, "u2");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task DeleteRecommendation_Success_Removes()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");
            var rec = new Recommendation { UserId = "u1", Title = "T", Description = "d", Type = RecommendationType.Movie };
            ctx.Recommendations.Add(rec); await ctx.SaveChangesAsync();

            var result = await Create(ctx).DeleteRecommendationAsync(rec.Id, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal(0, ctx.Recommendations.Count());
        }

        // ── RateRecommendationAsync ──────────────────────────────────────────
        [Fact]
        public async Task RateRecommendation_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();
            var result = await Create(ctx).RateRecommendationAsync(99, "u1", new RecommendationRatingCreateDto { Rating = 4 });
            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task RateRecommendation_NewRating_SetsAverage()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            var rec = new Recommendation { UserId = "u1", Title = "T", Description = "d", Type = RecommendationType.Movie };
            ctx.Recommendations.Add(rec); await ctx.SaveChangesAsync();

            await Create(ctx).RateRecommendationAsync(rec.Id, "u2", new RecommendationRatingCreateDto { Rating = 5 });

            Assert.Equal(5.0, ctx.Recommendations.First().AverageRating);
            Assert.Equal(1, ctx.Recommendations.First().TotalRatings);
        }

        [Fact]
        public async Task RateRecommendation_UpdatesExistingRating()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            var rec = new Recommendation { UserId = "u1", Title = "T", Description = "d", Type = RecommendationType.Movie };
            ctx.Recommendations.Add(rec); await ctx.SaveChangesAsync();
            ctx.RecommendationRatings.Add(new RecommendationRating { RecommendationId = rec.Id, UserId = "u2", Rating = 3 });
            await ctx.SaveChangesAsync();

            await Create(ctx).RateRecommendationAsync(rec.Id, "u2", new RecommendationRatingCreateDto { Rating = 5 });

            Assert.Equal(5, ctx.RecommendationRatings.First().Rating);
            Assert.Equal(1, ctx.Recommendations.First().TotalRatings);
        }
    }
}
