using System.ComponentModel.DataAnnotations;

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

    public class RecommendationFormDto
    {
        [Required]
        [MinLength(1)]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [MinLength(1)]
        [MaxLength(2000)]
        public string Description { get; set; } = null!;

        public int Type { get; set; }

        [MaxLength(100)]
        public string? Genre { get; set; }

        [MaxLength(200)]
        public string? Author { get; set; }

        [Range(1, 9999)]
        public int? Year { get; set; }

        [MaxLength(500)]
        public string? ExternalLink { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }
    }

    public class RecommendationRatingCreateDto
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
