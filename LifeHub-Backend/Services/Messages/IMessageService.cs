using LifeHub.DTOs;

namespace LifeHub.Services.Messages
{
    public interface IMessageService
    {
        Task<ServiceResult<PaginatedResult<MessageDto>>> GetConversationAsync(string userId, string otherUserId, int page = 1, int pageSize = 50);
        Task<ServiceResult<MessageDto>> SendMessageAsync(string userId, CreateMessageDto dto);
        Task<ServiceResult<MessageDto>> MarkAsReadAsync(int id, string userId);
        Task<ServiceResult<int>> GetUnreadCountAsync(string userId);
        Task<ServiceResult<Dictionary<string, int>>> GetUnreadCountPerSenderAsync(string userId);
    }
}
