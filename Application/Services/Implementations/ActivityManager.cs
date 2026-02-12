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

        public ActivityManager(IRepositoryManager manager, IMapper mapper)
        {
            _manager = manager;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ActivityDto>> GetRecentAsync(int count = 5, CancellationToken ct = default)
        {
            var activities = await _manager.Activity.GetRecentAsync(count, false);
            var activiesDto = _mapper.Map<IEnumerable<ActivityDto>>(activities);

            return activiesDto;
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
        }
    }
}
