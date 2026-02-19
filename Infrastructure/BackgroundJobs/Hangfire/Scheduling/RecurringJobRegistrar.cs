using Hangfire;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Maintenance;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Notifications;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Orders;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Outbox;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.BackgroundJobs.Hangfire.Scheduling
{
    public static class RecurringJobRegistrar
    {

        public static void Register(IRecurringJobManager recurringJobManager,
            IConfiguration configuration)
        {
            var options = configuration
                .GetSection("Hangfire")
                .Get<HangfireOptions>() ?? new HangfireOptions();

            if (!options.RecurringJobsEnabled)
                return;

            recurringJobManager.AddOrUpdate<PaymentPendingTimeoutJob>(
                recurringJobId: "orders:pending-timeout",
                methodCall: j => j.ExecuteAsync(20, 200, CancellationToken.None),
                cronExpression: "*/5 * * * *",
                options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc },
                queue: Queues.Orders);

            recurringJobManager.AddOrUpdate<NotificationDispatchJob>(
                recurringJobId: "notifications:dispatch",
                methodCall: j => j.ExecuteAsync(500, CancellationToken.None),
                cronExpression: "*/1 * * * *",
                options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc },
                queue: Queues.Notifications);

            recurringJobManager.AddOrUpdate<OutboxDispatcherJob>(
                recurringJobId: "outbox:dispatch",
                methodCall: j => j.ExecuteAsync(CancellationToken.None),
                cronExpression: "*/1 * * * *",
                options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc },
                queue: Queues.Outbox);

            recurringJobManager.AddOrUpdate<SoftDeleteCleanupJob>(
                recurringJobId: "maintenance:soft-delete-cleanup",
                methodCall: j => j.ExecuteAsync(CancellationToken.None),
                cronExpression: "30 1 * * *",
                options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc },
                queue: Queues.Maintenance);
        }
    }
}
