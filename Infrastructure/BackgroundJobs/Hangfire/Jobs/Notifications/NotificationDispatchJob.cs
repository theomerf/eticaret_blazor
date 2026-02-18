using Application.Repositories.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Infrastructure.BackgroundJobs.Hangfire.Jobs.Notifications
{
    public class NotificationDispatchJob
    {
        private readonly IRepositoryManager _manager;
        private readonly ILogger<NotificationDispatchJob> _logger;

        public NotificationDispatchJob(IRepositoryManager manager, ILogger<NotificationDispatchJob> logger)
        {
            _manager = manager;
            _logger = logger;
        }

        [Queue(Queues.Notifications)]
        public async Task ExecuteAsync(int batchSize = 500, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var pending = await _manager.Notification.GetPendingScheduledAsync(now, batchSize, trackChanges: true);
            if (pending.Count == 0)
                return;

            await _manager.ExecuteInTransactionAsync(async token =>
            {
                foreach (var notification in pending)
                {
                    notification.MarkAsSent();
                    _manager.Notification.Update(notification);
                }
            }, IsolationLevel.ReadCommitted, ct);

            _logger.LogInformation("Scheduled notifications dispatched. Count: {Count}", pending.Count);
        }
    }
}
