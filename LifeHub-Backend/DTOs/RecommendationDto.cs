namespace LifeHub.DTOs
{
    public class RecommendationDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Type { get; set; } // RecommendationType as int
        public string? Genre { get; set; }
        public string? Author { get; set; }
        public int? Year { get; set; }
        public string? ExternalLink { get; set; }
        public string? CoverImageUrl { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto? User { get; set; }
    }

    public class CreateRecommendationDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Type { get; set; }
        public string? Genre { get; set; }
        public string? Author { get; set; }
        public int? Year { get; set; }
        public string? ExternalLink { get; set; }
        public string? CoverImageUrl { get; set; }
    }

    public class UpdateRecommendationDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Type { get; set; }
        public string? Genre { get; set; }
        public string? Author { get; set; }
        public int? Year { get; set; }
        public string? ExternalLink { get; set; }
        public string? CoverImageUrl { get; set; }
    }

    public class RecommendationRatingCreateDto
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
