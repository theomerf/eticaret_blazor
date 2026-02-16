using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Repositories.Interfaces;
using Application.Queries.RequestParameters;
using Infrastructure.Persistence.Extensions;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class CouponRepository : RepositoryBase<Coupon>, ICouponRepository
    {
        public CouponRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<Coupon> coupons, int count)> GetAllAdminAsync(CouponRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default)
        {
            var query = FindAll(trackChanges)
                .FilterBy(p.IsActive, c => c.IsActive, FilterOperator.Equal)
                .FilterBy(p.Scope, c => c.Scope, FilterOperator.Equal)
                .FilterBy(p.Type, c => c.Type, FilterOperator.Equal);

            if (!string.IsNullOrWhiteSpace(p.SearchTerm))
            {
                var searchLower = p.SearchTerm.ToLower();
                query = query.Where(c => c.Code.ToLower().Contains(searchLower));
            }

            var count = await query.CountAsync(ct);

            query = p.SortBy switch
            {
                "code_asc" => query.OrderBy(c => c.Code),
                "code_desc" => query.OrderByDescending(c => c.Code),
                "date_asc" => query.OrderBy(c => c.CreatedAt),
                "end_asc" => query.OrderBy(c => c.EndsAt),
                "usage_desc" => query.OrderByDescending(c => c.UsedCount),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };

            var coupons = await query
                .ToPaginate(p.PageNumber, p.PageSize)
                .ToListAsync(ct);

            return (coupons, count);
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

        public async Task<Coupon?> GetByIdAsync(int couponId, bool trackChanges)
        {
            var coupon = await FindByCondition(c => c.CouponId == couponId, trackChanges)
                .FirstOrDefaultAsync();

            return coupon;
        }

        public async Task<Coupon?> GetByCodeAsync(string code, bool trackChanges)
        {
            var coupon = await FindByCondition(c => c.Code == code.ToUpper(), trackChanges)
                .FirstOrDefaultAsync();

            return coupon;
        }

        public async Task<Coupon?> GetWithUsagesAsync(int couponId, bool trackChanges)
        {
            var couponWithUsages = await FindByCondition(c => c.CouponId == couponId, trackChanges)
                .Include(c => c.Usages)
                .FirstOrDefaultAsync();

            return couponWithUsages;
        }

        public async Task<IEnumerable<Coupon>> GetActiveAsync(bool trackChanges)
        {
            var activeCoupons = await FindAllByCondition(
                c => c.IsActive && c.StartsAt <= DateTime.UtcNow && c.EndsAt >= DateTime.UtcNow,
                trackChanges)
                .OrderBy(c => c.Code)
                .ToListAsync();

            return activeCoupons;
        }

        public async Task<(IEnumerable<Coupon> coupons, int count)> GetPagedAsync(int pageNumber, int pageSize, bool trackChanges)
        {
            var query = FindAll(trackChanges);

            var count = await query.CountAsync();

            var coupons = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (coupons, count);
        }

        public async Task<bool> IsCouponCodeUniqueAsync(string code, int? excludeCouponId = null)
        {
            var query = FindAllByCondition(c => c.Code == code.ToUpper(), false);

            if (excludeCouponId.HasValue)
            {
                query = query.Where(c => c.CouponId != excludeCouponId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<int> GetUserCouponUsageCountAsync(int couponId, string userId)
        {
            var usageCount = await _context.CouponUsages
                .Where(cu => cu.CouponId == couponId && cu.UserId == userId)
                .CountAsync();

            return usageCount;
        }

        public void Create(Coupon coupon) => CreateEntity(coupon);

        public void Update(Coupon coupon) => UpdateEntity(coupon);

        public void Delete(Coupon coupon) => RemoveEntity(coupon);
    }
}
