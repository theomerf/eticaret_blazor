using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface INotificationRepository : IRepositoryBase<Notification>
    {
        Task<IEnumerable<Notification>> GetAllAsync(string userId, bool trackChanges);
        Task<Notification?> GetByIdAsync(int notificationId, bool trackChanges);
        void RemoveMultiple(IEnumerable<Notification> notifications);
        void UpdateMultiple(IEnumerable<Notification> notifications);
        void Create(Notification notification);
        void Remove(Notification notification);
        void Update(Notification notification);

    }
}
