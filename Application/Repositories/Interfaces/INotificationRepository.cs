using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface INotificationRepository : IRepositoryBase<Notification>
    {
        Task<IEnumerable<Notification>> GetAllNotificationsOfOneUserAsync(string userId, bool trackChanges);
        Task<Notification?> GetOneNotificationAsync(int notificationId, bool trackChanges);
        void RemoveNotifications(IEnumerable<Notification> notifications);
        void UpdateNotifications(IEnumerable<Notification> notifications);
        void CreateNotification(Notification notification);
        void RemoveNotification(Notification notification);
        void UpdateNotification(Notification notification);

    }
}
