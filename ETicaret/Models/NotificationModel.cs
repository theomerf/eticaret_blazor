namespace ETicaret.Models
{
    public class NotificationModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsClosing { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum NotificationType
    {
        Info,
        Success,
        Danger,
        Warning
    }
}