using LifeHub.DTOs;

namespace LifeHub.Services.Recommendations
{
    public interface IRecommendationService
    {
        Task<ServiceResult<List<RecommendationDto>>> GetRecommendationsAsync();
        Task<ServiceResult<RecommendationDto>> GetRecommendationAsync(int id);
        Task<ServiceResult<List<RecommendationDto>>> GetUserRecommendationsAsync(string userId);
        Task<ServiceResult<RecommendationDto>> CreateRecommendationAsync(string userId, CreateRecommendationDto dto);
        Task<ServiceResult<RecommendationDto>> UpdateRecommendationAsync(int id, string userId, UpdateRecommendationDto dto);
        Task<ServiceResult<bool>> DeleteRecommendationAsync(int id, string userId);
        Task<ServiceResult<bool>> RateRecommendationAsync(int id, string userId, RecommendationRatingCreateDto dto);
    }
}
