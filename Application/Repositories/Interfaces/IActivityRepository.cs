using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IActivityRepository : IRepositoryBase<Activity>
    {
        Task<IEnumerable<Activity>> GetRecentActivitiesAsync(int count, bool trackChanges);
        void CreateActivity(Activity activity);
    }
}
