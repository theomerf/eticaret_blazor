using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Repositories.Interfaces;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class CouponRepository : RepositoryBase<Coupon>, ICouponRepository
    {
        public CouponRepository(RepositoryContext context) : base(context)
        {
        }

        // Query Methods
        public async Task<Coupon?> GetCouponByIdAsync(int couponId, bool trackChanges)
        {
            var coupon = await FindByCondition(c => c.CouponId == couponId, trackChanges)
                .FirstOrDefaultAsync();

            return coupon;
        }

        public async Task<Coupon?> GetCouponByCodeAsync(string code, bool trackChanges)
        {
            var coupon = await FindByCondition(c => c.Code == code.ToUpper(), trackChanges)
                .FirstOrDefaultAsync();

            return coupon;
        }

        public async Task<Coupon?> GetCouponWithUsagesAsync(int couponId, bool trackChanges)
        {
            var couponWithUsages = await FindByCondition(c => c.CouponId == couponId, trackChanges)
                .Include(c => c.Usages)
                .FirstOrDefaultAsync();

            return couponWithUsages;
        }

        public async Task<IEnumerable<Coupon>> GetActiveCouponsAsync(bool trackChanges)
        {
            var activeCoupons = await FindAllByCondition(
                c => c.IsActive && c.StartsAt <= DateTime.UtcNow && c.EndsAt >= DateTime.UtcNow,
                trackChanges)
                .OrderBy(c => c.Code)
                .ToListAsync();

            return activeCoupons;
        }

        public async Task<IEnumerable<Coupon>> GetAllCouponsAsync(bool trackChanges)
        {
            var allCoupons = await FindAll(trackChanges)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return allCoupons;
        }

        public async Task<(IEnumerable<Coupon> coupons, int count)> GetCouponsPagedAsync(int pageNumber, int pageSize, bool trackChanges)
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

        public void CreateCoupon(Coupon coupon) => Create(coupon);

        public void UpdateCoupon(Coupon coupon) => Update(coupon);

        public void DeleteCoupon(Coupon coupon) => Remove(coupon);
    }
}
