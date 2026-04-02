using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using LifeHub.Data;
using LifeHub.Models;
using LifeHub.Utilidades;

namespace LifeHub.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                userId = Context.ConnectionId;

            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            await base.OnConnectedAsync();
        }

        public async Task SendMessageAsync(string receiverId, string content)
        {
            var senderId = Context.User.GetUserId();
            if (string.IsNullOrEmpty(senderId))
            {
                await Clients.Caller.SendAsync("Error", "Usuario no autenticado");
                return;
            }

            try
            {
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    SentAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Enviar mensaje al receptor
                await Clients.Group(receiverId).SendAsync("ReceiveMessage", new
                {
                    id = message.Id,
                    senderId = message.SenderId,
                    receiverId = message.ReceiverId,
                    content = message.Content,
                    sentAt = message.SentAt
                });

                // Confirmación al remitente
                await Clients.Caller.SendAsync("MessageSent", message.Id);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task MarkMessageAsReadAsync(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message != null)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await Clients.Group(message.SenderId).SendAsync("MessageRead", messageId);
            }
        }
    }
}
