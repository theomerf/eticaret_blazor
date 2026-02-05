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

        public void CreateActivity(Activity activity)
        {
            Create(activity);
        }

        public async Task<IEnumerable<Activity>> GetRecentActivitiesAsync(int count, bool trackChanges)
        {
            return await FindAll(trackChanges)
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
