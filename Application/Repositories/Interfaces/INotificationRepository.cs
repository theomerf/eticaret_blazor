using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface INotificationRepository : IRepositoryBase<Notification>
    {
        Task<(IReadOnlyList<NotificationAdminGroupDto> notifications, int count, int sentCount, int pendingCount)> GetAllAdminAsync(NotificationRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default);
        Task<IReadOnlyList<NotificationRecipientDto>> GetGroupRecipientsAsync(string groupId, int take, bool trackChanges, CancellationToken ct = default);
        Task<IEnumerable<Notification>> GetAllAsync(string userId, bool trackChanges);
        Task<Notification?> GetByIdAsync(int notificationId, bool trackChanges);
        Task<IReadOnlyList<Notification>> GetPendingScheduledAsync(DateTime utcNow, int take, bool trackChanges);
        void RemoveMultiple(IEnumerable<Notification> notifications);
        void UpdateMultiple(IEnumerable<Notification> notifications);
        void Create(Notification notification);
        void Remove(Notification notification);
        void Update(Notification notification);
    }
}
