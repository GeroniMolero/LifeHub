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
    public class MessagesController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public MessagesController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet("conversation/{otherUserId}")]
        public async Task<IActionResult> GetConversation(string otherUserId)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var messages = await _context.Messages
                .Where(m => 
                    (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                    (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return Ok(_mapper.Map<List<MessageDto>>(messages));
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var sessionError = await EnsureActiveSessionAsync(_context, userId);
            if (sessionError != null)
                return sessionError;

            var receiverExists = await _context.Users.AnyAsync(u => u.Id == dto.ReceiverId);
            if (!receiverExists)
                return BadRequestError("El usuario receptor no existe.");

            var message = new Message
            {
                SenderId = userId,
                ReceiverId = dto.ReceiverId,
                Content = dto.Content,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Created("", _mapper.Map<MessageDto>(message));
        }

        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
                return NotFoundError("Mensaje no encontrado.");

            if (message.ReceiverId != userId)
                return ForbiddenError("No tienes permisos para marcar este mensaje como leído.");

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(_mapper.Map<MessageDto>(message));
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadMessages()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var unreadCount = await _context.Messages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .CountAsync();

            return Ok(new { unreadCount });
        }
    }
}
