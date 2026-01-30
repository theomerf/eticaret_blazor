using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICampaignRepository
    {
        Task<Campaign?> GetCampaignByIdAsync(int campaignId, bool trackChanges);
        Task<IEnumerable<Campaign>> GetActiveCampaignsAsync(bool trackChanges);
        Task<IEnumerable<Campaign>> GetActiveCampaignsByPriorityAsync(bool trackChanges);
        Task<IEnumerable<Campaign>> GetAllCampaignsAsync(bool trackChanges);
        Task<(IEnumerable<Campaign> campaigns, int count)> GetCampaignsPagedAsync(int pageNumber, int pageSize, bool trackChanges);

        void CreateCampaign(Campaign campaign);
        void UpdateCampaign(Campaign campaign);
        void DeleteCampaign(Campaign campaign);
    }
}
