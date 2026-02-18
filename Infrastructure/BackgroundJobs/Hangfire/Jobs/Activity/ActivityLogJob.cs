using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities;
using Hangfire;

namespace Infrastructure.BackgroundJobs.Hangfire.Jobs.Activity
{
    public class ActivityLogJob
    {
        private readonly IRepositoryManager _manager;
        private readonly ICacheService _cache;

        public ActivityLogJob(IRepositoryManager manager, ICacheService cache)
        {
            _manager = manager;
            _cache = cache;
        }

        [Queue(Queues.Low)]
        public async Task ExecuteAsync(
            string title,
            string description,
            string icon,
            string colorClass,
            string? link = null,
            CancellationToken ct = default)
        {
            var activity = new Domain.Entities.Activity
            {
                Title = title,
                Description = description,
                Icon = icon,
                ColorClass = colorClass,
                Link = link,
                CreatedAt = DateTime.UtcNow
            };

            _manager.Activity.Create(activity);
            await _manager.SaveAsync();
            await _cache.RemoveByPrefixAsync("activities:", ct);
        }
    }
}
