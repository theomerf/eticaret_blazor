using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Repositories.Interfaces;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class CouponUsageRepository : RepositoryBase<CouponUsage>, ICouponUsageRepository
    {
        public CouponUsageRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CouponUsage>> GetCouponUsagesAsync(int couponId, bool trackChanges)
        {
            var couponUsages = await FindAllByCondition(cu => cu.CouponId == couponId, trackChanges)
                .Include(cu => cu.User)
                .Include(cu => cu.Order)
                .OrderByDescending(cu => cu.UsedAt)
                .ToListAsync();

            return couponUsages;
        }

        public async Task<IEnumerable<CouponUsage>> GetUserCouponUsagesAsync(string userId, bool trackChanges)
        {
            var couponUsages = await FindAllByCondition(cu => cu.UserId == userId, trackChanges)
                .Include(cu => cu.Coupon)
                .Include(cu => cu.Order)
                .OrderByDescending(cu => cu.UsedAt)
                .ToListAsync();

            return couponUsages;
        }

        public async Task<CouponUsage?> GetUserCouponUsageAsync(int couponId, string userId, bool trackChanges)
        {
            var couponUsage = await FindByCondition(
                cu => cu.CouponId == couponId && cu.UserId == userId,
                trackChanges)
                .FirstOrDefaultAsync();

            return couponUsage;
        }

        public void CreateCouponUsage(CouponUsage couponUsage) => Create(couponUsage);
    }
}
