using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services.Recommendations
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public RecommendationService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResult<List<RecommendationDto>>> GetRecommendationsAsync()
        {
            var recommendations = await _context.Recommendations
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<RecommendationDto>>.Ok(_mapper.Map<List<RecommendationDto>>(recommendations));
        }

        public async Task<ServiceResult<RecommendationDto>> GetRecommendationAsync(int id)
        {
            var recommendation = await _context.Recommendations
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recommendation == null)
                return ServiceResult<RecommendationDto>.NotFound("Recomendación no encontrada.");

            return ServiceResult<RecommendationDto>.Ok(_mapper.Map<RecommendationDto>(recommendation));
        }

        public async Task<ServiceResult<List<RecommendationDto>>> GetUserRecommendationsAsync(string userId)
        {
            var recommendations = await _context.Recommendations
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<RecommendationDto>>.Ok(_mapper.Map<List<RecommendationDto>>(recommendations));
        }

        public async Task<ServiceResult<RecommendationDto>> CreateRecommendationAsync(string userId, CreateRecommendationDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return ServiceResult<RecommendationDto>.Unauthorized("Sesión inválida. Inicia sesión de nuevo.");

            var recommendation = _mapper.Map<Recommendation>(dto);
            recommendation.UserId = userId;

            _context.Recommendations.Add(recommendation);
            await _context.SaveChangesAsync();

            return ServiceResult<RecommendationDto>.Ok(_mapper.Map<RecommendationDto>(recommendation));
        }

        public async Task<ServiceResult<RecommendationDto>> UpdateRecommendationAsync(int id, string userId, UpdateRecommendationDto dto)
        {
            var recommendation = await _context.Recommendations.FindAsync(id);

            if (recommendation == null)
                return ServiceResult<RecommendationDto>.NotFound("Recomendación no encontrada.");

            if (recommendation.UserId != userId)
                return ServiceResult<RecommendationDto>.Forbidden("No tienes permisos para actualizar esta recomendación.");

            _mapper.Map(dto, recommendation);
            recommendation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<RecommendationDto>.Ok(_mapper.Map<RecommendationDto>(recommendation));
        }

        public async Task<ServiceResult<bool>> DeleteRecommendationAsync(int id, string userId)
        {
            var recommendation = await _context.Recommendations.FindAsync(id);

            if (recommendation == null)
                return ServiceResult<bool>.NotFound("Recomendación no encontrada.");

            if (recommendation.UserId != userId)
                return ServiceResult<bool>.Forbidden("No tienes permisos para eliminar esta recomendación.");

            _context.Recommendations.Remove(recommendation);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> RateRecommendationAsync(int id, string userId, RecommendationRatingCreateDto dto)
        {
            var recommendation = await _context.Recommendations.FindAsync(id);

            if (recommendation == null)
                return ServiceResult<bool>.NotFound("Recomendación no encontrada.");

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
                _context.RecommendationRatings.Add(new RecommendationRating
                {
                    RecommendationId = id,
                    UserId = userId,
                    Rating = dto.Rating,
                    Comment = dto.Comment
                });
            }

            await _context.SaveChangesAsync();
            await UpdateAverageRatingAsync(recommendation);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        private async Task UpdateAverageRatingAsync(Recommendation recommendation)
        {
            var ratings = await _context.RecommendationRatings
                .Where(r => r.RecommendationId == recommendation.Id)
                .ToListAsync();

            if (ratings.Count > 0)
            {
                recommendation.AverageRating = ratings.Average(r => r.Rating);
                recommendation.TotalRatings = ratings.Count;
            }
        }
    }
}
