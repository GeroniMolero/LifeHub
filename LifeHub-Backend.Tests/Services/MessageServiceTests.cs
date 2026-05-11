using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using LifeHub.Services.Messages;
using LifeHub.Tests.Helpers;

namespace LifeHub.Tests.Services
{
    public class MessageServiceTests
    {
        private static MessageService Create(LifeHub.Data.ApplicationDbContext ctx) =>
            new(ctx, TestHelpers.CreateMapper());

        // ── GetConversationAsync ─────────────────────────────────────────────
        [Fact]
        public async Task GetConversation_ReturnsMessages_BothDirections()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");
            ctx.Messages.Add(new Message { SenderId = "u1", ReceiverId = "u2", Content = "hola" });
            ctx.Messages.Add(new Message { SenderId = "u2", ReceiverId = "u1", Content = "adios" });
            ctx.Messages.Add(new Message { SenderId = "u1", ReceiverId = "u3", Content = "otro" });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetConversationAsync("u1", "u2");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        [Fact]
        public async Task GetConversation_Empty_WhenNoMessages()
        {
            using var ctx = TestHelpers.CreateContext();
            var result = await Create(ctx).GetConversationAsync("u1", "u2");
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        // ── SendMessageAsync ─────────────────────────────────────────────────
        [Fact]
        public async Task SendMessage_InvalidSender_ReturnsUnauthorized()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).SendMessageAsync("ghost", new CreateMessageDto { ReceiverId = "u2", Content = "hi" });

            Assert.Equal(ServiceResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public async Task SendMessage_ReceiverNotFound_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1");

            var result = await Create(ctx).SendMessageAsync("u1", new CreateMessageDto { ReceiverId = "ghost", Content = "hi" });

            Assert.Equal(ServiceResultStatus.BadRequest, result.Status);
        }

        [Fact]
        public async Task SendMessage_Success_PersistsMessage()
        {
            using var ctx = TestHelpers.CreateContext();
            TestHelpers.AddUser(ctx, "u1"); TestHelpers.AddUser(ctx, "u2");

            var result = await Create(ctx).SendMessageAsync("u1", new CreateMessageDto { ReceiverId = "u2", Content = "hello" });

            Assert.True(result.IsSuccess);
            Assert.Equal("u1", result.Value!.SenderId);
            Assert.Equal("hello", result.Value.Content);
        }

        // ── MarkAsReadAsync ──────────────────────────────────────────────────
        [Fact]
        public async Task MarkAsRead_NotFound_ReturnsNotFound()
        {
            using var ctx = TestHelpers.CreateContext();

            var result = await Create(ctx).MarkAsReadAsync(99, "u2");

            Assert.Equal(ServiceResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task MarkAsRead_NotReceiver_ReturnsForbidden()
        {
            using var ctx = TestHelpers.CreateContext();
            var msg = new Message { SenderId = "u1", ReceiverId = "u2", Content = "x" };
            ctx.Messages.Add(msg); await ctx.SaveChangesAsync();

            var result = await Create(ctx).MarkAsReadAsync(msg.Id, "u1");

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task MarkAsRead_Success_SetsIsRead()
        {
            using var ctx = TestHelpers.CreateContext();
            var msg = new Message { SenderId = "u1", ReceiverId = "u2", Content = "x", IsRead = false };
            ctx.Messages.Add(msg); await ctx.SaveChangesAsync();

            var result = await Create(ctx).MarkAsReadAsync(msg.Id, "u2");

            Assert.True(result.IsSuccess);
            Assert.True(ctx.Messages.First().IsRead);
        }

        // ── GetUnreadCountAsync ──────────────────────────────────────────────
        [Fact]
        public async Task GetUnreadCount_ReturnsCorrectCount()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.Messages.Add(new Message { SenderId = "u1", ReceiverId = "u2", Content = "a", IsRead = false });
            ctx.Messages.Add(new Message { SenderId = "u1", ReceiverId = "u2", Content = "b", IsRead = true });
            ctx.Messages.Add(new Message { SenderId = "u1", ReceiverId = "u2", Content = "c", IsRead = false });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetUnreadCountAsync("u2");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value);
        }

        // ── GetUnreadCountPerSenderAsync ────────────────────────────────────
        [Fact]
        public async Task GetUnreadCountPerSender_GroupsByCorrectly()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.Messages.Add(new Message { SenderId = "u1", ReceiverId = "u3", Content = "a", IsRead = false });
            ctx.Messages.Add(new Message { SenderId = "u1", ReceiverId = "u3", Content = "b", IsRead = false });
            ctx.Messages.Add(new Message { SenderId = "u2", ReceiverId = "u3", Content = "c", IsRead = false });
            await ctx.SaveChangesAsync();

            var result = await Create(ctx).GetUnreadCountPerSenderAsync("u3");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!["u1"]);
            Assert.Equal(1, result.Value["u2"]);
        }
    }
}
