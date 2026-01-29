using Application.Repositories.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
    {
        public NotificationRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetAllNotificationsOfOneUserAsync(string userId, bool trackChanges)
        {
            var notifications = await FindAllByCondition(n => n.UserId == userId, false)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notifications;
        }

        public async Task<Notification?> GetOneNotificationAsync(int notificationId, bool trackChanges)
        {
            var notification = await FindByCondition(n => n.NotificationId == notificationId, false)
                .FirstOrDefaultAsync();

            return notification;
        }

        public void RemoveNotifications(IEnumerable<Notification> notifications)
        {
            RemoveRange(notifications);
        }

        public void UpdateNotifications(IEnumerable<Notification> notifications)
        {
            UpdateRange(notifications);
        }

        public void CreateNotification(Notification notification) 
        {
            Create(notification);
        }

        public void RemoveNotification(Notification notification) 
        {
            Remove(notification);
        }
        public void UpdateNotification(Notification notification) 
        {
            Update(notification);
        } 
    }
}
