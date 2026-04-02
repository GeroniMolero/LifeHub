using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RecommendationsController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public RecommendationsController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecommendations()
        {
            var recommendations = await _context.Recommendations
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(_mapper.Map<List<RecommendationDto>>(recommendations));
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecommendation(int id)
        {
            var recommendation = await _context.Recommendations
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recommendation == null)
                return NotFoundError("Recomendación no encontrada.");

            return Ok(_mapper.Map<RecommendationDto>(recommendation));
        }

        [HttpGet("user/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserRecommendations(string userId)
        {
            var recommendations = await _context.Recommendations
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(_mapper.Map<List<RecommendationDto>>(recommendations));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecommendation([FromBody] CreateRecommendationDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var sessionError = await EnsureActiveSessionAsync(_context, userId);
            if (sessionError != null)
                return sessionError;

            var recommendation = _mapper.Map<Recommendation>(dto);
            recommendation.UserId = userId;

            _context.Recommendations.Add(recommendation);
            await _context.SaveChangesAsync();

            return Created($"api/recommendations/{recommendation.Id}", _mapper.Map<RecommendationDto>(recommendation));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecommendation(int id, [FromBody] UpdateRecommendationDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var recommendation = await _context.Recommendations.FindAsync(id);

            if (recommendation == null)
                return NotFoundError("Recomendación no encontrada.");

            if (recommendation.UserId != userId)
                return ForbiddenError("No tienes permisos para actualizar esta recomendación.");

            _mapper.Map(dto, recommendation);
            recommendation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(_mapper.Map<RecommendationDto>(recommendation));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecommendation(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var recommendation = await _context.Recommendations.FindAsync(id);

            if (recommendation == null)
                return NotFoundError("Recomendación no encontrada.");

            if (recommendation.UserId != userId)
                return ForbiddenError("No tienes permisos para eliminar esta recomendación.");

            _context.Recommendations.Remove(recommendation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/rate")]
        public async Task<IActionResult> RateRecommendation(int id, [FromBody] RecommendationRatingCreateDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var recommendation = await _context.Recommendations.FindAsync(id);

            if (recommendation == null)
                return NotFoundError("Recomendación no encontrada.");

            var existingRating = await _context.RecommendationRatings
                .FirstOrDefaultAsync(r => r.RecommendationId == id && r.UserId == userId);

            if (existingRating != null)
            {
                existingRating.Rating = dto.Rating;
                existingRating.Comment = dto.Comment;
                existingRating.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var rating = new RecommendationRating
                {
                    RecommendationId = id,
                    UserId = userId,
                    Rating = dto.Rating,
                    Comment = dto.Comment
                };
                _context.RecommendationRatings.Add(rating);
            }

            await _context.SaveChangesAsync();
            UpdateRecommendationAverageRating(recommendation);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private void UpdateRecommendationAverageRating(Recommendation recommendation)
        {
            var ratings = _context.RecommendationRatings
                .Where(r => r.RecommendationId == recommendation.Id)
                .ToList();

            if (ratings.Any())
            {
                recommendation.AverageRating = ratings.Average(r => r.Rating);
                recommendation.TotalRatings = ratings.Count;
            }
        }
    }

    public class RecommendationRatingCreateDto
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
