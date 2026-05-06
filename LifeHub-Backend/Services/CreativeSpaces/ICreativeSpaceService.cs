using LifeHub.DTOs;

namespace LifeHub.Services.CreativeSpaces
{
    public interface ICreativeSpaceService
    {
        Task<ServiceResult<List<CreativeSpaceDto>>> GetCreativeSpacesAsync(string userId);
        Task<ServiceResult<CreativeSpaceDto>> GetCreativeSpaceAsync(int id, string userId);
        Task<ServiceResult<CreativeSpaceDto>> CreateCreativeSpaceAsync(string userId, CreateCreativeSpaceDto dto, string ipAddress);
        Task<ServiceResult<CreativeSpaceDto>> UpdateCreativeSpaceAsync(int id, string userId, UpdateCreativeSpaceDto dto, string ipAddress);
        Task<ServiceResult<bool>> DeleteCreativeSpaceAsync(int id, string userId, string ipAddress);
        Task<ServiceResult<bool>> SetFavoriteAsync(int id, string userId, bool isFavorite);
        Task<ServiceResult<List<SpacePermissionDto>>> GetPermissionsAsync(int id, string userId);
        Task<ServiceResult<SpacePermissionDto>> ShareAsync(int id, string userId, ShareCreativeSpaceDto dto, string ipAddress);
        Task<ServiceResult<bool>> RemovePermissionAsync(int id, string userId, string targetUserId, string ipAddress);
        Task<ServiceResult<List<SpaceMediaReferenceDto>>> GetMediaReferencesAsync(int id, string userId);
        Task<ServiceResult<SpaceMediaReferenceDto>> AddMediaReferenceAsync(int id, string userId, CreateSpaceMediaReferenceDto dto);
        Task<ServiceResult<bool>> RemoveMediaReferenceAsync(int id, string userId, string referenceId);
    }
}
