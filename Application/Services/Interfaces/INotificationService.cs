using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetAllNotificationsOfOneUserAsync(string userId);
        Task<OperationResult<NotificationDto>> MarkNotificationAsReadAsync(int notificationId);
        Task<OperationResult<NotificationDto>> MarkAllNotificationsAsReadAsync();
        Task<OperationResult<NotificationDto>> RemoveAllNotificationsAsync();
        Task<OperationResult<NotificationDto>> CreateNotificationAsync(NotificationDtoForCreation notificationDto);
        Task<OperationResult<NotificationDto>> RemoveNotificationAsync(int notificationId);
    }
}
