using Application.DTOs;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;

namespace Application.Services.Implementations
{
    public class ActivityManager : IActivityService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;

        public ActivityManager(IRepositoryManager manager, IMapper mapper, ICacheService cache)
        {
            _manager = manager;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<IEnumerable<ActivityDto>> GetRecentAsync(int count = 5, CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync(
                "activities:recent",
                async token =>
                {
                    var activities = await _manager.Activity.GetRecentAsync(count, false, token);
                    return _mapper.Map<IEnumerable<ActivityDto>>(activities);
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        }

        public async Task LogAsync(string title, string description, string icon, string colorClass, string? link = null)
        {
            var activity = new Activity
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
            await _cache.RemoveByPrefixAsync("activities:");
        }
    }
}
