using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetByUserIdAsync(string userId);
        Task<OperationResult<NotificationDto>> MarkAsReadAsync(int notificationId);
        Task<OperationResult<NotificationDto>> MarkAllAsReadAsync();
        Task<OperationResult<NotificationDto>> RemoveAllAsync();
        Task<OperationResult<NotificationDto>> CreateAsync(NotificationDtoForCreation notificationDto);
        Task<OperationResult<NotificationDto>> RemoveAsync(int notificationId);
    }
}
