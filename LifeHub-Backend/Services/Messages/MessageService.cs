using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services.Messages
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public MessageService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResult<List<MessageDto>>> GetConversationAsync(string userId, string otherUserId)
        {
            var messages = await _context.Messages
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                    (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return ServiceResult<List<MessageDto>>.Ok(_mapper.Map<List<MessageDto>>(messages));
        }

        public async Task<ServiceResult<MessageDto>> SendMessageAsync(string userId, CreateMessageDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return ServiceResult<MessageDto>.Unauthorized("Sesión inválida. Inicia sesión de nuevo.");

            var receiverExists = await _context.Users.AnyAsync(u => u.Id == dto.ReceiverId);
            if (!receiverExists)
                return ServiceResult<MessageDto>.BadRequest("El usuario receptor no existe.");

            var message = new Message
            {
                SenderId = userId,
                ReceiverId = dto.ReceiverId,
                Content = dto.Content,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return ServiceResult<MessageDto>.Ok(_mapper.Map<MessageDto>(message));
        }

        public async Task<ServiceResult<MessageDto>> MarkAsReadAsync(int id, string userId)
        {
            var message = await _context.Messages.FindAsync(id);

            if (message == null)
                return ServiceResult<MessageDto>.NotFound("Mensaje no encontrado.");

            if (message.ReceiverId != userId)
                return ServiceResult<MessageDto>.Forbidden("No tienes permisos para marcar este mensaje como leído.");

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ServiceResult<MessageDto>.Ok(_mapper.Map<MessageDto>(message));
        }

        public async Task<ServiceResult<int>> GetUnreadCountAsync(string userId)
        {
            var count = await _context.Messages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .CountAsync();

            return ServiceResult<int>.Ok(count);
        }

        public async Task<ServiceResult<Dictionary<string, int>>> GetUnreadCountPerSenderAsync(string userId)
        {
            var counts = await _context.Messages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new { SenderId = g.Key, Count = g.Count() })
                .ToListAsync();

            return ServiceResult<Dictionary<string, int>>.Ok(
                counts.ToDictionary(x => x.SenderId, x => x.Count));
        }
    }
}
