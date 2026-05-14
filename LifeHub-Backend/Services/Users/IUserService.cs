using LifeHub.DTOs;

namespace LifeHub.Services.Users
{
    public interface IUserService
    {
        Task<ServiceResult<PublicUserDto>> GetUserAsync(string id);
        Task<ServiceResult<UserDto>> GetCurrentUserAsync(string userId);
        Task<ServiceResult<List<UserDto>>> GetUsersAsync();
        Task<ServiceResult<List<PublicUserDto>>> SearchUsersAsync(string currentUserId, string? query);
        Task<ServiceResult<UserDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task<ServiceResult<bool>> DeleteCurrentUserAsync(string userId);
        Task<ServiceResult<bool>> DeleteUserAsync(string id, string callerUserId);
        Task<ServiceResult<bool>> ChangePasswordAsync(string userId, ChangePasswordDto dto);
    }
}
