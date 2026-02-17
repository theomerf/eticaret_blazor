using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Repositories.Interfaces;
using Application.Queries.RequestParameters;
using Infrastructure.Persistence.Extensions;
using Application.DTOs;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class CampaignRepository : RepositoryBase<Campaign>, ICampaignRepository
    {
        public CampaignRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<Campaign> campaigns, int count, int activeCount)> GetAllAdminAsync(CampaignRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default)
        {
            var query = FindAll(trackChanges)
                .FilterBy(p.IsActive, c => c.IsActive, FilterOperator.Equal)
                .FilterBy(p.Scope, c => c.Scope, FilterOperator.Equal)
                .FilterBy(p.Type, c => c.Type, FilterOperator.Equal);

            if (!string.IsNullOrWhiteSpace(p.SearchTerm))
            {
                var lower = p.SearchTerm.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(lower) ||
                    (c.Description != null && c.Description.ToLower().Contains(lower)));
            }

            var count = await query.CountAsync(ct);
            var activeCount = await query.CountAsync(c => c.IsActive, ct);

            query = p.SortBy switch
            {
                "name_asc" => query.OrderBy(c => c.Name),
                "name_desc" => query.OrderByDescending(c => c.Name),
                "date_asc" => query.OrderBy(c => c.CreatedAt),
                "priority_asc" => query.OrderBy(c => c.Priority),
                "priority_desc" => query.OrderByDescending(c => c.Priority),
                "end_asc" => query.OrderBy(c => c.EndsAt),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };

            var campaigns = await query
                .ToPaginate(p.PageNumber, p.PageSize)
                .ToListAsync(ct);

            return (campaigns, count, activeCount);
        }

        public async Task<int> CountOfActiveAsync(CancellationToken ct = default)
        {
            var count = await FindAllByCondition(
                c => c.IsActive && c.StartsAt <= DateTime.UtcNow && c.EndsAt >= DateTime.UtcNow,
                false
            )
            .CountAsync(ct);

            return count;
        }

        public async Task<Campaign?> GetByIdAsync(int campaignId, bool trackChanges)
        {
            var campaign = await FindByCondition(c => c.CampaignId == campaignId, trackChanges)
                .FirstOrDefaultAsync();

            return campaign;
        }

        public async Task<IEnumerable<Campaign>> GetActiveAsync(bool trackChanges)
        {
            var campaigns = await FindAllByCondition(
                c => c.IsActive && c.StartsAt <= DateTime.UtcNow && c.EndsAt >= DateTime.UtcNow,
                trackChanges)
                .OrderBy(c => c.Priority)
                .ToListAsync();

            return campaigns;
        }

        public async Task<IEnumerable<Campaign>> GetActiveByPriorityAsync(bool trackChanges)
        {
            var campaigns = await FindAllByCondition(
                c => c.IsActive && c.StartsAt <= DateTime.UtcNow && c.EndsAt >= DateTime.UtcNow,
                trackChanges)
                .OrderBy(c => c.Priority)
                .ThenByDescending(c => c.Value)
                .ToListAsync();

            return campaigns;
        }

        public async Task<(IEnumerable<Campaign> campaigns, int count)> GetPagedAsync(int pageNumber, int pageSize, bool trackChanges)
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

        public async Task<IEnumerable<CampaignUsageDto>> GetCampaignUsagesAsync(int campaignId, int take = 500, CancellationToken ct = default)
        {
            if (take <= 0) take = 500;

            var usages = await _context.OrderCampaigns
                .AsNoTracking()
                .Where(oc => oc.CampaignId == campaignId)
                .Include(oc => oc.Order)
                    .ThenInclude(o => o.User)
                .OrderByDescending(oc => oc.Order.OrderedAt)
                .Take(take)
                .Select(oc => new CampaignUsageDto
                {
                    OrderCampaignId = oc.OrderCampaignId,
                    CampaignId = oc.CampaignId,
                    OrderId = oc.OrderId,
                    OrderedAt = oc.Order.OrderedAt,
                    UserId = oc.Order.UserId,
                    UserEmail = oc.Order.User != null ? (oc.Order.User.Email ?? oc.Order.UserId) : oc.Order.UserId,
                    DiscountAmount = oc.DiscountAmount,
                    OrderTotalAmount = oc.Order.TotalAmount
                })
                .ToListAsync(ct);

            return usages;
        }

        public void Create(Campaign campaign) => CreateEntity(campaign);

        public void Update(Campaign campaign) => UpdateEntity(campaign);

        public void Delete(Campaign campaign) => RemoveEntity(campaign);
    }
}
