using Domain.Entities;

namespace Application.DTOs
{
    public record NotificationDto
    {
        public int NotificationId { get; set; }
        public NotificationType NotificationType { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
