using Application.Services.Interfaces;
using Hangfire;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Activity;

namespace Infrastructure.BackgroundJobs.Hangfire
{
    public class ActivityQueueService : IActivityQueueService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ActivityQueueService(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public string EnqueueLog(string title, string description, string icon, string colorClass, string? link = null)
            => _backgroundJobClient.Enqueue<ActivityLogJob>(j =>
                j.ExecuteAsync(title, description, icon, colorClass, link, CancellationToken.None));
    }
}
