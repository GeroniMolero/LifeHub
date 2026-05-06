using LifeHub.DTOs;

namespace LifeHub.Services.Messages
{
    public interface IMessageService
    {
        Task<ServiceResult<List<MessageDto>>> GetConversationAsync(string userId, string otherUserId);
        Task<ServiceResult<MessageDto>> SendMessageAsync(string userId, CreateMessageDto dto);
        Task<ServiceResult<MessageDto>> MarkAsReadAsync(int id, string userId);
        Task<ServiceResult<int>> GetUnreadCountAsync(string userId);
    }
}
