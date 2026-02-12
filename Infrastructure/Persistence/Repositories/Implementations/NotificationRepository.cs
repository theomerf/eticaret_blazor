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

        public async Task<IEnumerable<Notification>> GetAllAsync(string userId, bool trackChanges)
        {
            var notifications = await FindAllByCondition(n => n.UserId == userId, false)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notifications;
        }

        public async Task<Notification?> GetByIdAsync(int notificationId, bool trackChanges)
        {
            var notification = await FindByCondition(n => n.NotificationId == notificationId, false)
                .FirstOrDefaultAsync();

            return notification;
        }

        public void RemoveMultiple(IEnumerable<Notification> notifications)
        {
            RemoveRange(notifications);
        }

        public void UpdateMultiple(IEnumerable<Notification> notifications)
        {
            UpdateRange(notifications);
        }

        public void Create(Notification notification) 
        {
            CreateEntity(notification);
        }

        public void Remove(Notification notification) 
        {
            RemoveEntity(notification);
        }
        public void Update(Notification notification) 
        {
            UpdateEntity(notification);
        } 
    }
}
