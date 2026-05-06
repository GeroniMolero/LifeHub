using LifeHub.DTOs;

namespace LifeHub.Services.Friendships
{
    public interface IFriendshipService
    {
        Task<ServiceResult<List<FriendshipDto>>> GetFriendshipsAsync(string userId);
        Task<ServiceResult<List<FriendshipDto>>> GetAcceptedFriendsAsync(string userId);
        Task<ServiceResult<FriendshipDto>> SendFriendRequestAsync(string userId, CreateFriendshipDto dto);
        Task<ServiceResult<FriendshipDto>> UpdateFriendshipAsync(int id, string userId, UpdateFriendshipDto dto);
        Task<ServiceResult<bool>> DeleteFriendshipAsync(int id, string userId);
    }
}
