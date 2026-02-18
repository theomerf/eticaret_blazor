using Application.DTOs;

namespace Application.Services.Interfaces
{
    public interface INotificationQueueService
    {
        string EnqueueCreate(NotificationDtoForCreation notificationDto);
        string EnqueueCreateBulk(NotificationDtoForBulkCreation notificationDto, string createdByUserId);
    }
}
