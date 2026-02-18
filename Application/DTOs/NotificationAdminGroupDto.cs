using Domain.Entities;

namespace Application.DTOs
{
    public record NotificationAdminGroupDto
    {
        public string GroupId { get; set; } = null!;
        public NotificationType NotificationType { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsSent { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool SentToAllActiveUsers { get; set; }
        public int RecipientCount { get; set; }
        public List<string> RecipientPreviewEmails { get; set; } = [];
    }
}
