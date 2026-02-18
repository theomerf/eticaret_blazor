using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface INotificationService
    {
        Task<(IReadOnlyList<NotificationAdminGroupDto> notifications, int count, int sentCount, int pendingCount)> GetAllAdminAsync(NotificationRequestParametersAdmin p, CancellationToken ct = default);
        Task<IReadOnlyList<NotificationRecipientDto>> GetGroupRecipientsAsync(string groupId, int take = 200, CancellationToken ct = default);
        Task<IEnumerable<NotificationDto>> GetByUserIdAsync(string userId);
        Task<OperationResult<NotificationDto>> MarkAsReadAsync(int notificationId);
        Task<OperationResult<NotificationDto>> MarkAllAsReadAsync();
        Task<OperationResult<NotificationDto>> RemoveAllAsync();
        Task<OperationResult<NotificationDto>> CreateAsync(NotificationDtoForCreation notificationDto);
        Task<OperationResult<NotificationDto>> CreateBulkNotificationAsync(NotificationDtoForBulkCreation notificationDto);
        Task<OperationResult<NotificationDto>> RemoveAsync(int notificationId);
    }
}
