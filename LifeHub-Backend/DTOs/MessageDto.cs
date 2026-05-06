using System.ComponentModel.DataAnnotations;

namespace LifeHub.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class CreateMessageDto
    {
        [Required]
        public string ReceiverId { get; set; } = null!;

        [Required]
        [MinLength(1)]
        [MaxLength(5000)]
        public string Content { get; set; } = null!;
    }
}
