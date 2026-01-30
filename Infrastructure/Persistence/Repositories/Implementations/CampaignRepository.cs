using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Repositories.Interfaces;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class CampaignRepository : RepositoryBase<Campaign>, ICampaignRepository
    {
        public CampaignRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<Campaign?> GetCampaignByIdAsync(int campaignId, bool trackChanges)
        {
            var campaign = await FindByCondition(c => c.CampaignId == campaignId, trackChanges)
                .FirstOrDefaultAsync();

            return campaign;
        }

        public async Task<IEnumerable<Campaign>> GetActiveCampaignsAsync(bool trackChanges)
        {
            var campaigns = await FindAllByCondition(
                c => c.IsActive && c.StartsAt <= DateTime.UtcNow && c.EndsAt >= DateTime.UtcNow,
                trackChanges)
                .OrderBy(c => c.Priority)
                .ToListAsync();

            return campaigns;
        }

        public async Task<IEnumerable<Campaign>> GetActiveCampaignsByPriorityAsync(bool trackChanges)
        {
            var campaigns = await FindAllByCondition(
                c => c.IsActive && c.StartsAt <= DateTime.UtcNow && c.EndsAt >= DateTime.UtcNow,
                trackChanges)
                .OrderBy(c => c.Priority)
                .ThenByDescending(c => c.Value)
                .ToListAsync();

            return campaigns;
        }

        public async Task<IEnumerable<Campaign>> GetAllCampaignsAsync(bool trackChanges)
        {
            var campaigns = await FindAll(trackChanges)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return campaigns;
        }

        public async Task<(IEnumerable<Campaign> campaigns, int count)> GetCampaignsPagedAsync(int pageNumber, int pageSize, bool trackChanges)
        {
            var query = FindAll(trackChanges);

            var count = await query.CountAsync();

            var campaigns = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (campaigns, count);
        }

        public void CreateCampaign(Campaign campaign) => Create(campaign);

        public void UpdateCampaign(Campaign campaign) => Update(campaign);

        public void DeleteCampaign(Campaign campaign) => Remove(campaign);
    }
}
