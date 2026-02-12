using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IActivityRepository : IRepositoryBase<Activity>
    {
        Task<IEnumerable<Activity>> GetRecentAsync(int count, bool trackChanges, CancellationToken ct = default);
        void Create(Activity activity);
    }
}
