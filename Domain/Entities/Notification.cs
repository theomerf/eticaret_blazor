using Domain.Exceptions;

namespace Domain.Entities
{
    public class Notification : AuditableEntity
    {
        public int NotificationId { get; set; }
        public NotificationType NotificationType { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsRead { get; set; } = false;
        public string UserId { get; set; } = null!;
        public User? User { get; set; }

        public bool IsSystemGenerated { get; set; } = true; // Sistem mi oluşturdu, admin mi?
        public DateTime? ScheduledFor { get; set; } // Zamanlanmış bildirim
        public bool IsSent { get; set; } = false; // Gönderildi mi?
        public string? NotificationGroupId { get; set; }
        public bool SentToAllActiveUsers { get; set; } = false;

        #region Validation Methods

        public void ValidateForCreation()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                throw new NotificationValidationException("Bildirim başlığı boş olamaz.");
            }

            if (Title.Length < 3 || Title.Length > 200)
            {
                throw new NotificationValidationException("Bildirim başlığı 3-200 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                throw new NotificationValidationException("Bildirim açıklaması boş olamaz.");
            }

            if (Description.Length < 5 || Description.Length > 500)
            {
                throw new NotificationValidationException("Bildirim açıklaması 5-500 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(UserId))
            {
                throw new NotificationValidationException("Kullanıcı ID'si boş olamaz.");
            }
        }

        #endregion

        #region Business Logic Methods

        public void MarkAsRead()
        {
            IsRead = true;
        }

        public void MarkAsUnread()
        {
            IsRead = false;
        }

        public void MarkAsSent()
        {
            IsSent = true;
        }

        public void SoftDelete(string deletedByUserId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
        }

        #endregion
    }

    public enum NotificationType
    {
        Order,
        Shipment,
        Campaign,
        Settings,
        Payment,
        System,
        Admin,
        Other
    }
}
