using LifeHub.DTOs;
using LifeHub.Services.Recommendations;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RecommendationsController : ApiControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationsController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecommendations()
        {
            var result = await _recommendationService.GetRecommendationsAsync();
            return ToActionResult(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecommendation(int id)
        {
            var result = await _recommendationService.GetRecommendationAsync(id);
            return ToActionResult(result);
        }

        [HttpGet("user/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserRecommendations(string userId)
        {
            var result = await _recommendationService.GetUserRecommendationsAsync(userId);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecommendation([FromBody] RecommendationFormDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _recommendationService.CreateRecommendationAsync(userId, dto);
            if (!result.IsSuccess) return ToActionResult(result);

            return Created($"api/recommendations/{result.Value!.Id}", result.Value);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecommendation(int id, [FromBody] RecommendationFormDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _recommendationService.UpdateRecommendationAsync(id, userId, dto);
            return ToActionResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecommendation(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _recommendationService.DeleteRecommendationAsync(id, userId);
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }

        [HttpPost("{id}/rate")]
        public async Task<IActionResult> RateRecommendation(int id, [FromBody] RecommendationRatingCreateDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _recommendationService.RateRecommendationAsync(id, userId, dto);
            if (!result.IsSuccess) return ToActionResult(result);

            return Ok();
        }
    }
}
