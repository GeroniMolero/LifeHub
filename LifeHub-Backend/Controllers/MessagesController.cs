using LifeHub.DTOs;
using LifeHub.Services.Messages;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ApiControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet("conversation/{otherUserId}")]
        public async Task<IActionResult> GetConversation(string otherUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _messageService.GetConversationAsync(userId, otherUserId, page, pageSize);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _messageService.SendMessageAsync(userId, dto);
            if (!result.IsSuccess) return ToActionResult(result);

            return Created("", result.Value);
        }

        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _messageService.MarkAsReadAsync(id, userId);
            return ToActionResult(result);
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadMessages()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _messageService.GetUnreadCountAsync(userId);
            if (!result.IsSuccess) return ToActionResult(result);

            return Ok(new { unreadCount = result.Value });
        }

        [HttpGet("unread-per-sender")]
        public async Task<IActionResult> GetUnreadPerSender()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _messageService.GetUnreadCountPerSenderAsync(userId);
            return ToActionResult(result);
        }
    }
}
