using Application.DTOs;
using Application.Services.Interfaces;
using Hangfire;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Notifications;

namespace Infrastructure.BackgroundJobs.Hangfire
{
    public class NotificationQueueService : INotificationQueueService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public NotificationQueueService(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public string EnqueueCreate(NotificationDtoForCreation notificationDto)
            => _backgroundJobClient.Enqueue<NotificationCreateJob>(j =>
                j.CreateAsync(notificationDto, CancellationToken.None));

        public string EnqueueCreateBulk(NotificationDtoForBulkCreation notificationDto, string createdByUserId)
            => _backgroundJobClient.Enqueue<NotificationCreateJob>(j =>
                j.CreateBulkAsync(notificationDto, createdByUserId, CancellationToken.None));
    }
}
