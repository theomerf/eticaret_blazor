using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICampaignRepository
    {
        Task<IEnumerable<Campaign>> GetAllAsync(bool trackChanges);
        Task<Campaign?> GetByIdAsync(int campaignId, bool trackChanges);
        Task<IEnumerable<Campaign>> GetActiveAsync(bool trackChanges);
        Task<IEnumerable<Campaign>> GetActiveByPriorityAsync(bool trackChanges);
        Task<(IEnumerable<Campaign> campaigns, int count)> GetPagedAsync(int pageNumber, int pageSize, bool trackChanges);
        void Create(Campaign campaign);
        void Update(Campaign campaign);
        void Delete(Campaign campaign);
    }
}
