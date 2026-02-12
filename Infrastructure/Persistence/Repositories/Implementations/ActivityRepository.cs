using Application.Repositories.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class ActivityRepository : RepositoryBase<Activity>, IActivityRepository
    {
        public ActivityRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Activity>> GetRecentAsync(int count, bool trackChanges, CancellationToken ct = default)
        {
            var activities = await FindAll(trackChanges)
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync(ct);

            return activities;
        }

        public void Create(Activity activity)
        {
            CreateEntity(activity);
        }
    }
}
