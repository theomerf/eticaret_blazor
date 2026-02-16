using Domain.Entities;
using Application.DTOs;
using Application.Queries.RequestParameters;

namespace Application.Repositories.Interfaces
{
    public interface ICampaignRepository
    {
        Task<(IEnumerable<Campaign> campaigns, int count)> GetAllAdminAsync(CampaignRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default);
        Task<int> CountOfActiveAsync(CancellationToken ct = default);
        Task<Campaign?> GetByIdAsync(int campaignId, bool trackChanges);
        Task<IEnumerable<Campaign>> GetActiveAsync(bool trackChanges);
        Task<IEnumerable<Campaign>> GetActiveByPriorityAsync(bool trackChanges);
        Task<(IEnumerable<Campaign> campaigns, int count)> GetPagedAsync(int pageNumber, int pageSize, bool trackChanges);
        Task<IEnumerable<CampaignUsageDto>> GetCampaignUsagesAsync(int campaignId, int take = 500, CancellationToken ct = default);
        void Create(Campaign campaign);
        void Update(Campaign campaign);
        void Delete(Campaign campaign);
    }
}
