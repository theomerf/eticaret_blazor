using Application.DTOs;
using Application.Repositories.Interfaces;
using Domain.Entities;
using Hangfire;

namespace Infrastructure.BackgroundJobs.Hangfire.Jobs.Notifications
{
    public class NotificationCreateJob
    {
        private readonly IRepositoryManager _manager;

        public NotificationCreateJob(IRepositoryManager manager)
        {
            _manager = manager;
        }

        [Queue(Queues.Notifications)]
        public async Task CreateAsync(NotificationDtoForCreation notificationDto, CancellationToken ct = default)
        {
            var notification = new Notification
            {
                NotificationType = notificationDto.NotificationType,
                Title = notificationDto.Title,
                Description = notificationDto.Description,
                UserId = notificationDto.UserId,
                ScheduledFor = notificationDto.ScheduledFor,
                IsSystemGenerated = true,
                IsSent = true
            };

            notification.ValidateForCreation();

            _manager.Notification.Create(notification);
            await _manager.SaveAsync();
        }

        [Queue(Queues.Notifications)]
        public async Task CreateBulkAsync(NotificationDtoForBulkCreation notificationDto, string createdByUserId, CancellationToken ct = default)
        {
            var batchSize = Math.Clamp(notificationDto.BatchSize, 100, 5000);
            var groupId = Guid.NewGuid().ToString("N");

            if (notificationDto.SendToAllUsers)
            {
                var page = 1;
                while (!ct.IsCancellationRequested)
                {
                    var activeUserIds = await _manager.User.GetActiveUserIdsBatchAsync(page, batchSize, trackChanges: false, ct);
                    if (activeUserIds.Count == 0)
                        break;

                    foreach (var userId in activeUserIds)
                    {
                        var notification = BuildBulkNotification(notificationDto, createdByUserId, userId, groupId);
                        notification.ValidateForCreation();
                        _manager.Notification.Create(notification);
                    }

                    await _manager.SaveAsync();
                    _manager.ClearTracker();
                    page++;
                }

                return;
            }

            var targetUserIds = notificationDto.UserIds
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .ToArray();

            foreach (var userIdBatch in targetUserIds.Chunk(batchSize))
            {
                foreach (var userId in userIdBatch)
                {
                    var notification = BuildBulkNotification(notificationDto, createdByUserId, userId, groupId);
                    notification.ValidateForCreation();
                    _manager.Notification.Create(notification);
                }

                await _manager.SaveAsync();
                _manager.ClearTracker();
            }
        }

        private static Notification BuildBulkNotification(NotificationDtoForBulkCreation notificationDto, string createdByUserId, string userId, string groupId)
        {
            return new Notification
            {
                NotificationType = notificationDto.NotificationType,
                Title = notificationDto.Title,
                Description = notificationDto.Description,
                UserId = userId,
                IsSystemGenerated = false,
                CreatedByUserId = createdByUserId,
                UpdatedByUserId = createdByUserId,
                NotificationGroupId = groupId,
                SentToAllActiveUsers = notificationDto.SendToAllUsers,
                ScheduledFor = notificationDto.ScheduledFor,
                IsSent = notificationDto.ScheduledFor == null
            };
        }
    }
}
